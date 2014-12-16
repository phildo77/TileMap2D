using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY
using UnityEngine;
#endif

namespace ioSoftSmiths.ioVector
{
    public struct IVector2
    {

        public int x, y;

        public static readonly IVector2 Zero = new IVector2(0, 0);

        //Axis Directions
        public static readonly IVector2 XnYp = new IVector2(-1, 1);
        public static readonly IVector2 Yp = new IVector2(0, 1);
        public static readonly IVector2 XpYp = new IVector2(1, 1);
        public static readonly IVector2 Xp = new IVector2(1, 0);
        public static readonly IVector2 XpYn = new IVector2(1, -1);
        public static readonly IVector2 Yn = new IVector2(0, -1);
        public static readonly IVector2 XnYn = new IVector2(-1, -1);
        public static readonly IVector2 Xn = new IVector2(-1, 0);
        public static readonly List<IVector2> DirsCardCW = new List<IVector2>() { Yp, Xp, Yn, Xn };
        public static readonly List<IVector2> DirsAllCW = new List<IVector2>() {XnYp, Yp, XpYp, Xp, XpYn, Yn, XnYn, Xn}; 

        //Neighbors
        public IVector2 Xnext { get { return new IVector2(x + 1, y); } }
        public IVector2 Xprev { get { return new IVector2(x - 1, y); } }
        public IVector2 Ynext { get { return new IVector2(x, y + 1); } }
        public IVector2 Yprev { get { return new IVector2(x, y - 1); } }
        public IVector2[] Neighbors(bool _includeDiags = false)
        {
            if(_includeDiags) 
                return new IVector2[8] 
                {
                    new IVector2(x + 1, y),
                    new IVector2(x - 1, y),
                    new IVector2(x, y + 1),
                    new IVector2(x, y - 1),
                    new IVector2(x - 1, y - 1),
                    new IVector2(x + 1, y - 1),
                    new IVector2(x - 1, y + 1),
                    new IVector2(x + 1, y + 1)
                };
            else
                return new IVector2[4] 
                {
                    new IVector2(x + 1, y),
                    new IVector2(x - 1, y),
                    new IVector2(x, y + 1),
                    new IVector2(x, y - 1)
                };
        }


        public IVector2(int _x, int _y)
        {
            x = _x;
            y = _y;
        }

        public IVector2(IVector2 _coord)
        {
            x = _coord.x;
            y = _coord.y;
        }


        public override int GetHashCode()
        {
            return x ^ y;
        }

        public bool Equals(IVector2 vector)
        {
            // Return true if the fields match:
            return (x == vector.x) && (y == vector.y);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }


        public static bool operator ==(IVector2 a, IVector2 b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(IVector2 a, IVector2 b)
        {
            return !(a == b);
        }

        public static IVector2 operator +(IVector2 a, IVector2 b)
        {
            return new IVector2(a.x + b.x, a.y + b.y);
        }

        public static IVector2 operator -(IVector2 a, IVector2 b)
        {
            return new IVector2(a.x - b.x, a.y - b.y);
        }

        public static IVector2 operator -(IVector2 a)
        {
            return new IVector2(-a.x, -a.y);
        }

        public static IVector2 operator *(IVector2 a, int b)
        {
            return new IVector2(a.x * b, a.y * b);
        }

        public static IVector2 operator *(int a, IVector2 b)
        {
            return new IVector2(a * b.x, a * b.y);
        }

        public override string ToString()
        {
            return x + ", " + y ;
        }

        public double Magnitude { get { return Math.Sqrt(((double)x * x) + (double)(y * y)); } }

        public double[] Normalized {
            get {
                double abMag = Math.Abs(Magnitude);
                return new double[2] { (double)x / abMag, (double)y / abMag };
            }
        }

    }





}