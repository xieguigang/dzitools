Imports System.Drawing
Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Features2D
Imports Emgu.CV.Structure
Imports Emgu.CV.Util

Public Class CellRegistrationRANSAC
    ' 细胞数据结构
    Public Structure CellData
        Public Position As PointF           ' 细胞核位置
        Public MajorAxis As Single         ' 长轴长度
        Public MinorAxis As Single         ' 短轴长度
        Public Orientation As Single       ' 旋转角度（弧度）
        Public Area As Double              ' 细胞面积
    End Structure

    ' 变换结果
    Public Structure RegistrationResult
        Public TransformationMatrix As Mat ' 变换矩阵
        Public InlierCount As Integer      ' 内点数量
        Public FitnessScore As Double       ' 配准得分
        Public InlierMatches As List(Of DMatch) ' 内点匹配对
    End Structure

    ''' <summary>
    ''' 提取细胞特征描述符
    ''' </summary>
    Public Function ExtractCellFeatures(cells As List(Of CellData)) As Matrix(Of Single)
        ' 每个细胞用5维特征向量表示：[X, Y, MajorAxis, MinorAxis, Area]
        Dim features As New Matrix(Of Single)(cells.Count, 5)

        For i As Integer = 0 To cells.Count - 1
            features(i, 0) = cells(i).Position.X
            features(i, 1) = cells(i).Position.Y
            features(i, 2) = cells(i).MajorAxis
            features(i, 3) = cells(i).MinorAxis
            features(i, 4) = CSng(cells(i).Area)
        Next

        Return features
    End Function

    ''' <summary>
    ''' 使用RANSAC进行细胞点云配准
    ''' </summary>
    Public Function RegisterCellsWithRANSAC(
        sourceCells As List(Of CellData),
        targetCells As List(Of CellData),
        Optional ransacReprojThreshold As Double = 5.0,
        Optional confidence As Double = 0.99,
        Optional maxIters As Integer = 2000) As RegistrationResult

        ' 1. 提取特征
        Dim sourceFeatures As Matrix(Of Single) = ExtractCellFeatures(sourceCells)
        Dim targetFeatures As Matrix(Of Single) = ExtractCellFeatures(targetCells)

        ' 2. 特征匹配（使用FLANN匹配器）
        Dim matches As New VectorOfDMatch()
        Using matcher As New FlannBasedMatcher()
            ' 转换特征矩阵为适合匹配的格式
            Dim sourceDescriptors As New Mat()
            Dim targetDescriptors As New Mat()
            CvInvoke.Convert(sourceFeatures.Mat, sourceDescriptors, DepthType.Cv32F)
            CvInvoke.Convert(targetFeatures.Mat, targetDescriptors, DepthType.Cv32F)

            matcher.Add(targetDescriptors)
            matcher.KnnMatch(sourceDescriptors, New VectorOfVectorOfDMatch(matches), 2)
        End Using

        ' 3. 筛选初步匹配（比率测试）
        Dim goodMatches As New List(Of DMatch)()
        For Each matchArray As DMatch() In matches.ToArray()
            If matchArray.Length >= 2 AndAlso matchArray(0).Distance < 0.7 * matchArray(1).Distance Then
                goodMatches.Add(matchArray(0))
            End If
        Next

        ' 4. 准备RANSAC输入数据
        Dim srcPoints As New List(Of PointF)()
        Dim dstPoints As New List(Of PointF)()

        For Each match As DMatch In goodMatches
            srcPoints.Add(sourceCells(match.QueryIdx).Position)
            dstPoints.Add(targetCells(match.TrainIdx).Position)
        Next

        If srcPoints.Count < 4 Then
            Throw New Exception("匹配点数量不足，至少需要4个点进行RANSAC配准")
        End If

        ' 5. 执行RANSAC配准
        Dim homography As New Mat()
        Dim inliers As New Mat()

        CvInvoke.FindHomography(
            PointCollectionToMat(srcPoints),
            PointCollectionToMat(dstPoints),
            homography,
            HomographyMethod.Ransac,
            ransacReprojThreshold,
            inliers,
            maxIters,
            confidence)

        ' 6. 计算内点数量
        Dim inlierCount As Integer = 0
        If Not inliers.IsEmpty Then
            For i As Integer = 0 To inliers.Rows - 1
                If inliers.GetData(i, 0) <> 0 Then
                    inlierCount += 1
                End If
            Next
        End If

        ' 7. 计算配准得分
        Dim fitnessScore As Double = If(goodMatches.Count > 0, inlierCount / CDbl(goodMatches.Count), 0)

        ' 8. 识别内点匹配
        Dim inlierMatches As New List(Of DMatch)()
        If Not inliers.IsEmpty Then
            For i As Integer = 0 To inliers.Rows - 1
                If inliers.GetData(i, 0) <> 0 AndAlso i < goodMatches.Count Then
                    inlierMatches.Add(goodMatches(i))
                End If
            Next
        End If

        ' 9. 返回结果
        Dim result As New RegistrationResult With {
            .TransformationMatrix = homography,
            .InlierCount = inlierCount,
            .FitnessScore = fitnessScore,
            .InlierMatches = inlierMatches
        }

        Return result
    End Function

    ''' <summary>
    ''' 将点集合转换为Mat
    ''' </summary>
    Private Function PointCollectionToMat(points As List(Of PointF)) As Mat
        Dim pointArray(points.Count - 1) As PointF
        points.CopyTo(pointArray)
        Return New Mat(pointArray.Length, 1, DepthType.Cv32F, 2)
    End Function

    ''' <summary>
    ''' 应用变换矩阵到细胞点云
    ''' </summary>
    Public Function TransformCells(cells As List(Of CellData), homography As Mat) As List(Of CellData)
        Dim transformedCells As New List(Of CellData)()

        For Each cell As CellData In cells
            ' 变换细胞位置
            Dim srcPoint As New Mat(1, 1, DepthType.Cv32F, 3)
            srcPoint.SetTo(New Single() {cell.Position.X, cell.Position.Y, 1.0F})

            Dim dstPoint As Mat = homography * srcPoint
            Dim result As Single() = dstPoint.GetData()

            ' 齐次坐标归一化
            Dim newX As Single = result(0) / result(2)
            Dim newY As Single = result(1) / result(2)

            ' 创建变换后的细胞数据（形态参数保持不变）
            Dim transformedCell As CellData = cell
            transformedCell.Position = New PointF(newX, newY)
            transformedCells.Add(transformedCell)
        Next

        Return transformedCells
    End Function

    ''' <summary>
    ''' 完整的配准流程示例
    ''' </summary>
    Public Sub CompleteRegistrationExample()
        ' 示例数据 - 实际使用时替换为您的细胞数据
        Dim sourceCells As New List(Of CellData)()
        Dim targetCells As New List(Of CellData)()

        ' 这里添加您的细胞数据...
        ' sourceCells.Add(New CellData With {...})
        ' targetCells.Add(New CellData With {...})

        Try
            ' 执行RANSAC配准
            Dim result As RegistrationResult = RegisterCellsWithRANSAC(sourceCells, targetCells)

            ' 应用变换
            Dim registeredCells As List(Of CellData) = TransformCells(sourceCells, result.TransformationMatrix)

            ' 输出结果
            Console.WriteLine($"配准完成！内点数量: {result.InlierCount}, 配准得分: {result.FitnessScore:P2}")
            Console.WriteLine($"变换矩阵: {result.TransformationMatrix.GetData()}") ' 这里需要根据矩阵格式调整输出

            ' 这里可以添加结果可视化或保存代码

        Catch ex As Exception
            Console.WriteLine($"配准失败: {ex.Message}")
        End Try
    End Sub
End Class