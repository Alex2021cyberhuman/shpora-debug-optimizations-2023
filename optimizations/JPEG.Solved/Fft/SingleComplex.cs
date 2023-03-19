using System;

namespace JPEG.Solved.Fft;

public readonly record struct SingleComplex(
    float Real,
    float Imaginary)
{
    public string ToString(
        string format,
        IFormatProvider formatProvider)
    {
        return $"{Real} {(Imaginary < 0 ? '-' : '+')} {Imaginary}i";
    }

    public float Magnitude => MathF.Sqrt(Real * Real + Imaginary * Imaginary);
    public float Phase => MathF.Atan(Imaginary / Real);


    public static SingleComplex operator -(
        SingleComplex value) /* Unary negation of a complex number */
    {
        return new(-value.Real, -value.Imaginary);
    }

    public static SingleComplex operator +(
        SingleComplex left,
        SingleComplex right)
    {
        return new(left.Real + right.Real, left.Imaginary + right.Imaginary);
    }

    public static SingleComplex operator +(
        SingleComplex left,
        float right)
    {
        return left with { Real = left.Real + right };
    }

    public static SingleComplex operator +(
        float left,
        SingleComplex right)
    {
        return right with { Real = left + right.Real };
    }

    public static SingleComplex operator -(
        SingleComplex left,
        SingleComplex right)
    {
        return new(left.Real - right.Real, left.Imaginary - right.Imaginary);
    }

    public static SingleComplex operator -(
        SingleComplex left,
        float right)
    {
        return left with { Real = left.Real - right };
    }

    public static SingleComplex operator -(
        float left,
        SingleComplex right)
    {
        return new(left - right.Real, -right.Imaginary);
    }

    public static SingleComplex operator *(
        SingleComplex left,
        SingleComplex right)
    {
        // Multiplication:  (a + bi)(c + di) = (ac -bd) + (bc + ad)i
        var resultRealPart = left.Real * right.Real -
                             left.Imaginary * right.Imaginary;
        var resultImaginaryPart = left.Imaginary * right.Real +
                                  left.Real * right.Imaginary;
        return new(resultRealPart, resultImaginaryPart);
    }

    public static SingleComplex One { get; } = new(1f, 0f);
    public static SingleComplex Zero { get; } = new(0f, 0f);
    public static SingleComplex NegativeOne { get; } = new(-1f, 0f);
}