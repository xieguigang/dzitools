' 首先，在文件顶部添加必要的 Imports
Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Structure
Imports Emgu.CV.Util
Imports System.Drawing

' 定义一个结构体来存储您的细胞信息
Public Structure CellInfo
    Public X As Single
    Public Y As Single
    Public MajorAxis As Single ' 长轴
    Public MinorAxis As Single ' 短轴
    Public Angle As Single     ' 旋转角度
    Public Area As Single      ' 面积

    Public Sub New(x As Single, y As Single, majorAxis As Single, minorAxis As Single, angle As Single, area As Single)
        Me.X = x
        Me.Y = y
        Me.MajorAxis = majorAxis
        Me.MinorAxis = minorAxis
        Me.Angle = angle
        Me.Area = area
    End Sub
End Structure

Module RansacAlignment

    ''' <summary>
    ''' 使用 RANSAC 算法对齐两组细胞点云
    ''' </summary>
    ''' <param name="sourceCells">源切片的细胞信息列表（将被变换）</param>
    ''' <param name="destinationCells">目标切片的细胞信息列表（对齐的目标）</param>
    ''' <param name="ransacThreshold">RANSAC 重投影阈值（像素）。小于此值的点对被视为内点。通常设置为 3-5 像素。</param>
    ''' <returns>一个元组，包含：
    ''' 1. transformMatrix: 计算出的 2x3 变换矩阵。如果失败则为 Nothing。
    ''' 2. inliersMask: 内点掩码，与输入点同大小，非零值表示为内点。如果失败则为 Nothing。
    ''' </returns>
    ''' <remarks>
    ''' **重要**: 此函数假设 sourceCells 和 destinationCells 中的点已经按顺序一一对应。
    ''' 在实际应用中，您需要先进行特征匹配来建立这种对应关系。
    ''' </remarks>
    Public Function AlignCellClouds(
        sourceCells As List(Of CellInfo),
        destinationCells As List(Of CellInfo),
        Optional ransacThreshold As Double = 5.0) As Tuple(Of Mat, Mat)

        ' 检查输入数据
        If sourceCells Is Nothing OrElse destinationCells Is Nothing OrElse
           sourceCells.Count <> destinationCells.Count OrElse sourceCells.Count < 2 Then
            Console.WriteLine("错误：输入的点集无效或点数不足（至少需要2个点）。")
            Return New Tuple(Of Mat, Mat)(Nothing, Nothing)
        End If

        ' 1. 数据准备：将 CellInfo 列表转换为 Emgu CV 的 VectorOfPointF
        Dim sourcePoints As New VectorOfPointF()
        Dim destPoints As New VectorOfPointF()

        Call sourcePoints.Push((From cell As CellInfo In sourceCells Select New PointF(cell.X, cell.Y)).ToArray)
        Call destPoints.Push((From cell As CellInfo In destinationCells Select New PointF(cell.X, cell.Y)).ToArray)

        ' 2. RANSAC 参数设置
        ' inliersMask 将用于存储哪些点对是内点
        Dim inliersMask As New Mat()
        ' 使用部分仿射变换，它对平移、旋转和均匀缩放很鲁棒
        Dim method As RobustEstimationAlgorithm = RobustEstimationAlgorithm.Ransac
        ' 最大迭代次数，默认值通常足够
        Dim maxIters As Integer = 2000
        ' 置信度，默认值通常足够
        Dim confidence As Double = 0.99

        ' 3. 执行 RANSAC 对齐
        ' EstimateAffinePartial2D 非常适合此场景，因为它排除了剪切变换，更符合物理切片的移动和旋转
        Dim transformMatrix As Mat = CvInvoke.EstimateAffinePartial2D(
            sourcePoints,
            destPoints,
            inliersMask,
            method,
            ransacThreshold,
            maxIters,
            confidence, 100)

        ' 4. 检查结果
        If transformMatrix Is Nothing OrElse transformMatrix.IsEmpty Then
            Console.WriteLine("错误：无法计算变换矩阵。可能是点对应关系错误或阈值设置不当。")
            Return New Tuple(Of Mat, Mat)(Nothing, Nothing)
        End If

        Console.WriteLine("成功计算变换矩阵。")
        Console.WriteLine("变换矩阵内容:")
        Console.WriteLine(transformMatrix)

        ' 统计内点数量
        Dim inlierCount As Integer = CvInvoke.CountNonZero(inliersMask)
        Console.WriteLine($"找到 {inlierCount} / {sourceCells.Count} 个内点。")

        Return New Tuple(Of Mat, Mat)(transformMatrix, inliersMask)

    End Function

    ''' <summary>
    ''' 主函数，用于演示如何使用对齐功能
    ''' </summary>
    Public Sub Run()
        ' --- 模拟数据 ---
        ' 假设我们有两张切片的细胞信息，并且已经完成了特征匹配，点的顺序是对应的。

        ' 目标切片（基准）
        Dim destinationCells As New List(Of CellInfo) From {
            New CellInfo(100, 100, 20, 15, 0, 300),
            New CellInfo(200, 150, 22, 18, 10, 320),
            New CellInfo(150, 250, 19, 16, -5, 290),
            New CellInfo(300, 200, 21, 17, 15, 310),
            New CellInfo(120, 180, 18, 14, 30, 280)
        }

        ' 源切片（相对于目标切片，经过了平移、旋转和缩放，并加入了一些噪声）
        Dim sourceCells As New List(Of CellInfo)()
        Dim angleRad As Single = Math.PI / 6 ' 旋转 30 度
        Dim scale As Single = 1.1F ' 放大 1.1 倍
        Dim tx As Single = 50 ' X方向平移
        Dim ty As Single = -30 ' Y方向平移

        For Each dCell In destinationCells
            ' 应用变换
            Dim cos_a As Single = CSng(Math.Cos(angleRad))
            Dim sin_a As Single = CSng(Math.Sin(angleRad))
            Dim x_centered As Single = dCell.X - 200 ' 假设绕 (200, 200) 旋转
            Dim y_centered As Single = dCell.Y - 200

            Dim x_rotated As Single = x_centered * cos_a - y_centered * sin_a
            Dim y_rotated As Single = x_centered * sin_a + y_centered * cos_a

            Dim x_scaled As Single = x_rotated * scale
            Dim y_scaled As Single = y_rotated * scale

            Dim x_final As Single = x_scaled + 200 + tx
            Dim y_final As Single = y_scaled + 200 + ty

            ' 加入一些随机噪声
            Dim rnd As New Random()
            sourceCells.Add(New CellInfo(
                x_final + rnd.Next(-2, 2),
                y_final + rnd.Next(-2, 2),
                dCell.MajorAxis * scale,
                dCell.MinorAxis * scale,
                dCell.Angle + 30,
                dCell.Area * scale * scale))
        Next

        ' 添加一些异常值，这些点在另一张切片上没有对应点
        sourceCells.Add(New CellInfo(50, 50, 10, 10, 0, 100)) ' 异常值 1
        sourceCells.Add(New CellInfo(400, 400, 25, 20, 45, 400)) ' 异常值 2
        destinationCells.Add(New CellInfo(0, 0, 0, 0, 0, 0)) ' 为保持列表长度一致，添加一个占位符
        destinationCells.Add(New CellInfo(0, 0, 0, 0, 0, 0)) ' 为保持列表长度一致，添加一个占位符


        Console.WriteLine("源切片细胞数量: " & sourceCells.Count)
        Console.WriteLine("目标切片细胞数量: " & destinationCells.Count)
        Console.WriteLine("--- 开始对齐 ---")

        ' 调用对齐函数
        Dim alignmentResult As Tuple(Of Mat, Mat) = AlignCellClouds(sourceCells, destinationCells, ransacThreshold:=5.0)

        If alignmentResult.Item1 IsNot Nothing Then
            Dim transformMatrix As Mat = alignmentResult.Item1
            Dim inliersMask As Mat = alignmentResult.Item2

            Console.WriteLine("--- 对齐成功，应用变换 ---")

            ' 5. 应用变换到源点云，以验证结果
            Dim sourcePointsMat As New Matrix(Of Single)(sourceCells.Count, 1, DepthType.Cv32F, 2)
            For i As Integer = 0 To sourceCells.Count - 1
                sourcePointsMat.Data(i, 0) = sourceCells(i).X
                sourcePointsMat.Data(i, 1) = sourceCells(i).Y
            Next

            Dim transformedPointsMat As New Mat()
            CvInvoke.Transform(sourcePointsMat, transformedPointsMat, transformMatrix)

            ' 将变换后的 Mat 转换为 Matrix(Of Single) 以便轻松读取数据
            Dim transformedMatrix As New Matrix(Of Single)(transformedPointsMat.Rows, transformedPointsMat.Cols, transformedPointsMat.NumberOfChannels)

            Call transformedPointsMat.CopyTo(transformedMatrix)

            ' --- 修改点 3: 使用 Matrix(Of Byte) 来读取内点掩码 ---
            Dim inlierMaskMatrix As New Matrix(Of Byte)(inliersMask.Rows, inliersMask.Cols, inliersMask.NumberOfChannels)

            Call inliersMask.CopyTo(inlierMaskMatrix)

            ' 打印一些变换前后的点进行对比
            Console.WriteLine(vbCrLf & "变换前后对比 (仅显示前几个点):")
            For i As Integer = 0 To Math.Min(4, sourceCells.Count - 1)
                Dim originalPoint As String = $"({sourceCells(i).X:F1}, {sourceCells(i).Y:F1})"
                Dim transformedX As Single = transformedMatrix.Data(i, 0)
                Dim transformedY As Single = transformedMatrix.Data(i, 1)
                Dim transformedPoint As String = $"({transformedX:F1}, {transformedY:F1})"
                Dim targetPoint As String = $"({destinationCells(i).X:F1}, {destinationCells(i).Y:F1})"
                Dim isInlier As String = If(inlierMaskMatrix.Data(i, 0) > 0, "内点", "异常值")
                Console.WriteLine($"点 {i}: 源 {originalPoint} -> 变换后 {transformedPoint} | 目标 {targetPoint} ({isInlier})")
            Next
        End If

        Console.WriteLine(vbCrLf & "按任意键退出...")
        Console.ReadKey()
    End Sub

End Module
