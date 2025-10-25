Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Emgu.CV.Util

Module Module1

    Public Function Polygon2DToMat(ByVal polygon As Polygon2D) As Matrix(Of Single)
        ' 创建一个N行2列的矩阵，数据类型为32位浮点数
        Dim pointCount As Integer = polygon.xpoints.Length
        Dim pointMat As New Matrix(Of Single)(pointCount, 2, DepthType.Cv32F, 1)

        ' 将点数据填充到矩阵中
        Dim index As Integer = 0
        For i As Integer = 0 To pointCount - 1
            pointMat(i, 0) = CSng(polygon.xpoints(i)) ' X坐标
            pointMat(i, 1) = CSng(polygon.ypoints(i)) ' Y坐标
        Next

        Return pointMat
    End Function
End Module
