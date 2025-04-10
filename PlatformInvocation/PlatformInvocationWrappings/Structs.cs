using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PlatformInvokationWrappings
{
    public static class Structs
    {
        public readonly struct POINT<T1> where T1 : struct, INumber<T1>
        {
            public static POINT<T2> New<T2>(T2 x, T2 y) where T2 : struct, INumber<T2>
                => new(x, y);

            public static POINT<double> NewMax<T2>(T1 x, T2 y) where T2 : struct, INumber<T2>
                => (x.D(), y.D());


            private POINT(T1 x, T1 y)
            {
                if (!TypeHierarchy.Contains(typeof(T1)))
                    throw new ArgumentException($"Type {typeof(T1).FullName} is unsupported.");
                X = x;
                Y = y;
            }

            public readonly T1 X;
            public readonly T1 Y;
            public Type InternalType => typeof(T1);

            public POINT<double> AddVector<T2>(POINT<T2> secondPoint) where T2 : struct, INumber<T2>
                => new(
                    double.CreateChecked(X) + double.CreateChecked(secondPoint.X),
                    double.CreateChecked(Y) + double.CreateChecked(secondPoint.Y));

            public POINT<double> SubtractVector<T2>(POINT<T2> secondPoint) where T2 : struct, INumber<T2>
                => new(
                    double.CreateChecked(X) - double.CreateChecked(secondPoint.X),
                    double.CreateChecked(Y) - double.CreateChecked(secondPoint.Y));

            public POINT<double> MultiplyVector<T2>(POINT<T2> secondPoint) where T2 : struct, INumber<T2>
                => new(
                    double.CreateChecked(X) - double.CreateChecked(secondPoint.X),
                    double.CreateChecked(Y) - double.CreateChecked(secondPoint.Y));

            public POINT<double> DivideVector<T2>(POINT<T2> secondPoint) where T2 : struct, INumber<T2>
                => new(
                    double.CreateChecked(X) / double.CreateChecked(secondPoint.X),
                    double.CreateChecked(Y) / double.CreateChecked(secondPoint.Y));


            public static POINT<T1> operator +(POINT<T1> left, POINT<T1> right)
                => new(left.X + right.X, left.Y + right.Y);

            public static POINT<T1> operator -(POINT<T1> left, POINT<T1> right)
                => new(left.X - right.X, left.Y - right.Y);

            public static POINT<T1> operator *(POINT<T1> left, POINT<T1> right)
                => new(left.X * right.X, left.Y * right.Y);

            public static POINT<T1> operator /(POINT<T1> left, POINT<T1> right)
                => new(left.X / right.X, left.Y / right.Y);

            public static bool operator ==(POINT<T1> left, POINT<T1> right)
                => left.Equals(right);

            public static bool operator !=(POINT<T1> left, POINT<T1> right) 
                => !(left == right);

            public new bool Equals([NotNullWhen(true)] object? obj)
            {
                if (obj is POINT<T1> point)
                    return point.X == X && point.Y == Y;
                return base.Equals(obj);
            }

            public bool Equals<T2>(POINT<T2> point) where T2 : struct, INumber<T2>
                =>  X.D().AlmostEqualTo(point.X.D()) && Y.D().AlmostEqualTo(point.Y.D());

            public bool EqualsStrict<T2>(POINT<T2> point) where T2 : struct, INumber<T2>
                => X.D().Equals(point.X.D()) && Y.D().Equals(point.Y.D());

            // ReSharper disable once StaticMemberInGenericType
            private static readonly ImmutableList<Type> TypeHierarchy =
                [typeof(byte), typeof(short), typeof(int), typeof(float), typeof(long), typeof(double)];

            public bool IsLarger<T2>() where T2 : struct, INumber<T2>
                => TypeHierarchy.IndexOf(typeof(T2)) > TypeHierarchy.IndexOf(typeof(T1)) ? true : false;

            public static implicit operator (double, double)(POINT<T1> point)
                => (double.CreateChecked(point.X), double.CreateChecked(point.Y));

            public static implicit operator POINT<T1>((byte, byte) point)
                => new(T1.CreateChecked(point.Item1), T1.CreateChecked(point.Item2));

            public static implicit operator POINT<T1>((short, short) point)
                => new(T1.CreateChecked(point.Item1), T1.CreateChecked(point.Item2));

            public static implicit operator POINT<T1>((int, int) point)
                => new(T1.CreateChecked(point.Item1), T1.CreateChecked(point.Item2));

            public static implicit operator POINT<T1>((float, float) point)
                => new(T1.CreateChecked(point.Item1), T1.CreateChecked(point.Item2));

            public static implicit operator POINT<T1>((double, double) point)
                => new(T1.CreateChecked(point.Item1), T1.CreateChecked(point.Item2));

            public static implicit operator POINT<T1>((sbyte, sbyte) point)
                => new(T1.CreateChecked(point.Item1), T1.CreateChecked(point.Item2));

            public static implicit operator POINT<T1>((ushort, ushort) point)
                => new(T1.CreateChecked(point.Item1), T1.CreateChecked(point.Item2));

            public static implicit operator POINT<T1>((uint, uint) point)
                => new(T1.CreateChecked(point.Item1), T1.CreateChecked(point.Item2));

            public static implicit operator POINT<T1>((long, long) point)
                => new(T1.CreateChecked(point.Item1), T1.CreateChecked(point.Item2));

            public static implicit operator POINT<T1>((ulong, ulong) point)
                => new(T1.CreateChecked(point.Item1), T1.CreateChecked(point.Item2));

            public static implicit operator POINT<T1>(System.Drawing.Point point)
                => new(T1.CreateChecked(point.X), T1.CreateChecked(point.Y));

            public static implicit operator System.Drawing.Point(POINT<T1> point)
                => new(int.CreateChecked(point.X), int.CreateChecked(point.Y));

            public override string ToString()
            {
                return $"[{X}, {Y}]";
            }
        }
    }
}
