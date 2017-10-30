Imports System.Globalization
Namespace Lolzzz
    Public Class IntConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim i As Integer
            If Integer.TryParse(value, i) Then
                Return i
            Else
                Return Binding.DoNothing
            End If
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Return value.ToString()
        End Function
    End Class

    Public Class DecimalConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim i As Double
            If Double.TryParse(value, i) Then
                Return i
            Else
                Return Binding.DoNothing
            End If
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Return value.ToString()
        End Function
    End Class

End Namespace
