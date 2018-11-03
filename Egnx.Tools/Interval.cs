using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Egnx.Tools
{
    public class Interval<T> : IComparable
    //,IComparable<T>
    {
        private static Regex regex = new Regex(@"^(?<sbound>\[|\])(?<begin>[^\;]+)?(?:;)(?<end>[^\;]+)?(?<ebound>\[|\])$", RegexOptions.Compiled);

        public T Begin { get; set; }
        public T End { get; set; }
        public bool IsBeginIncluded { get; set; }
        public bool IsEndIncluded { get; set; }

        public Interval() : this(true, true)
        {

        }

        public Interval(bool isstartincluded, bool isendincluded)
        {
            Init(isstartincluded, isendincluded);
        }

        public Interval(T value, bool isstartincluded = true, bool isendincluded = true) : this(isstartincluded, isendincluded)
        {
            //Init(isstartincluded, isendincluded);
            Begin = value;
            End = value;
        }

        private void Init(bool isstartincluded, bool isendincluded)
        {
            IsBeginIncluded = isstartincluded;
            IsEndIncluded = isendincluded;
        }

        public Interval<T> Clone()
        {
            return new Interval<T>() { Begin = this.Begin, End = this.End, IsEndIncluded = this.IsEndIncluded, IsBeginIncluded = this.IsBeginIncluded };
        }

        public static Interval<T> operator +(T value, Interval<T> interval)
        {
            return interval + value;
        }

        public static Interval<T> operator +(Interval<T> interval, T value)
        {
            var result = interval.Clone();
            result.Begin = (dynamic)result.Begin + (dynamic)value;
            result.End = (dynamic)result.End + (dynamic)value;
            return result;

        }

        public Interval(T begin, T end, bool isstartincluded = true, bool isendincluded = true)
        {
            Begin = begin;
            End = end;
            IsBeginIncluded = isstartincluded;
            IsEndIncluded = isendincluded;
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1};{2}{3}",
                IsBeginIncluded ? "[" : "]",
                Begin,
                End,
                IsEndIncluded ? "]" : "["
                );
        }

        public static Interval<T> Parse(string value)
        {

            if (string.IsNullOrEmpty(value))
            {
                throw new Exception("Value is empty.");
            }

            var match = regex.Match(value);

            var result = match.Success;

            if (result)
            {

                var isstartincluded = match.Groups["sbound"].Value == "[";
                var isendincluded = match.Groups["ebound"].Value == "]";

                var start = (T)ChangeType(match.Groups["begin"].Value);
                var end = (T)ChangeType(match.Groups["end"].Value);
                return new Interval<T>(start, end, isstartincluded, isendincluded);
            }
            else
            {
                throw new Exception("Invalid string format.");
            }
        }

        private static T ChangeType(string value)
        {
            var type = typeof(T);
            if (string.IsNullOrEmpty(value) && !type.IsNullable())
            {
                throw new Exception("Only nullable types accept empty value.");
            }
            if (string.IsNullOrEmpty(value) && type.IsNullable())
            {
                return default(T);
            }

            var dsttype = type.IsNullable() ? Nullable.GetUnderlyingType(type) : type;

            return (T)Convert.ChangeType(value.Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, "."), dsttype, CultureInfo.InvariantCulture);

        }

        public T Middle()
        {
            var type = typeof(T);
            if (type.IsNullable() && (Begin == null || End == null))
            {
                return default(T);
            }
            return ((dynamic)Begin + (dynamic)End) / (dynamic)2;
        }

        public bool IsInside(T value)
        {
            bool result = false;
            var type = typeof(T);
            if (type.IsNullable() && (Begin == null || End == null))
            {
                if (End != null)
                {
                    result = IsEndIncluded ? (value as IComparable).CompareTo(End) <= 0 : (value as IComparable).CompareTo(End) < 0;
                }
                else if (Begin != null)
                {
                    result = IsBeginIncluded ? (value as IComparable).CompareTo(Begin) >= 0 : (value as IComparable).CompareTo(Begin) > 0;
                }
                else //[-inf;+inf] !
                {
                    result = true;
                }

            }
            else
            {
                result = IsBeginIncluded ? (value as IComparable).CompareTo(Begin) >= 0 : (value as IComparable).CompareTo(Begin) > 0;
                if (result)
                {
                    result = IsEndIncluded ? (value as IComparable).CompareTo(End) <= 0 : (value as IComparable).CompareTo(End) < 0;
                }
            }
            return result;
        }

        public int PositionIndex(T value, int intervals)
        {
            if (Begin != null && End != null)
            {
                var delta = (dynamic)End - (dynamic)Begin;
                //delta /= (dynamic) intervals;

                var x = ((dynamic)value - (dynamic)Begin) / delta;
                var idx = Math.Round((intervals - 1) * x);
                return ((int)idx);

            }
            return -1;
        }


        public int Position(T value)
        {
            int result = 0;
            if (IsInside(value))
            {
                return result;
            }

            var type = typeof(T);
            if (type.IsNullable() && (Begin == null || End == null))
            {
                if (End != null)
                {
                    result = 1;
                }
                else if (Begin != null)
                {
                    result = -1;
                }
                //else //[-inf;+inf] !
                //{
                //    result = true;
                //}

            }
            else
            {
                var boundok = IsBeginIncluded ? (value as IComparable).CompareTo(Begin) >= 0 : (value as IComparable).CompareTo(Begin) > 0;
                if (!boundok)
                {
                    result = -1;
                }
                else
                {
                    result = 1;
                }
            }
            return result;
        }


        public int CompareTo(object obj)
        {

            if (obj == null)
            {
                throw new Exception("Object cannot be null");
            }


            var aobj = (obj as Interval<T>);
            int result;
            if (End != null && aobj.End != null)
            {
                result = (this.End as IComparable).CompareTo(aobj.End);

                if (result == 0)
                {
                    if (IsEndIncluded && !aobj.IsEndIncluded)
                    {
                        result = 1;
                    }
                    else if (!IsEndIncluded && aobj.IsEndIncluded)
                    {
                        result = -1;
                    }
                }
                return result;
            }

            if (End == null && aobj.End == null)
            {
                return 0;
            }

            if (End == null)
            {
                return 1;
            }

            //if (aobj.End == null)
            //{
            return -1;
            //}

        }

        //public int CompareTo(T other)
        //{
        //    return CompareTo((object)other);
        //}
    }

}
