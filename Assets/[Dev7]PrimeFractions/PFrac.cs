using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PFrac
{
    private bool s;
    private List<byte> n;
    private List<byte> d;

    #region Constructors
    public PFrac(List<byte> numerator, List<byte> denominator)
    {
        s = true;
        n = numerator;
        d = denominator;
    }

    public PFrac(bool sign, List<byte> numerator, List<byte> denominator)
    {
        s = sign;
        n = numerator;
        d = denominator;
    }

    public PFrac(float decimalNum)
    {
        s = true;
        n = new();
        d = new();

        FloatToPFrac(decimalNum, out s, out n, out d);
    }
    #endregion

    #region Prime list
    //Credits: https://gist.github.com/davidjpfeiffer/155112b11ee243b9b536c6ac70cfcf49
    public static List<int> PrimeList = new() { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293, 307, 311, 313, 317, 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397, 401, 409, 419, 421, 431, 433, 439, 443, 449, 457, 461, 463, 467, 479, 487, 491, 499, 503, 509, 521, 523, 541, 547, 557, 563, 569, 571, 577, 587, 593, 599, 601, 607, 613, 617, 619, 631, 641, 643, 647, 653, 659, 661, 673, 677, 683, 691, 701, 709, 719, 727, 733, 739, 743, 751, 757, 761, 769, 773, 787, 797, 809, 811, 821, 823, 827, 829, 839, 853, 857, 859, 863, 877, 881, 883, 887, 907, 911, 919, 929, 937, 941, 947, 953, 967, 971, 977, 983, 991, 997, 1009, 1013, 1019, 1021, 1031, 1033, 1039, 1049, 1051, 1061, 1063, 1069, 1087, 1091, 1093, 1097, 1103, 1109, 1117, 1123, 1129, 1151, 1153, 1163, 1171, 1181, 1187, 1193, 1201, 1213, 1217, 1223, 1229, 1231, 1237, 1249, 1259, 1277, 1279, 1283, 1289, 1291, 1297, 1301, 1303, 1307, 1319, 1321, 1327, 1361, 1367, 1373, 1381, 1399, 1409, 1423, 1427, 1429, 1433, 1439, 1447, 1451, 1453, 1459, 1471, 1481, 1483, 1487, 1489, 1493, 1499, 1511, 1523, 1531, 1543, 1549, 1553, 1559, 1567, 1571, 1579, 1583, 1597, 1601, 1607, 1609, 1613, 1619, 1621, 1627, 1637, 1657, 1663, 1667, 1669, 1693, 1697, 1699, 1709, 1721, 1723, 1733, 1741, 1747, 1753, 1759, 1777, 1783, 1787, 1789, 1801, 1811, 1823, 1831, 1847, 1861, 1867, 1871, 1873, 1877, 1879, 1889, 1901, 1907, 1913, 1931, 1933, 1949, 1951, 1973, 1979, 1987, 1993, 1997, 1999, 2003, 2011, 2017, 2027, 2029, 2039, 2053, 2063, 2069, 2081, 2083, 2087, 2089, 2099, 2111, 2113, 2129, 2131, 2137, 2141, 2143, 2153, 2161, 2179, 2203, 2207, 2213, 2221, 2237, 2239, 2243, 2251, 2267, 2269, 2273, 2281, 2287, 2293, 2297, 2309, 2311, 2333, 2339, 2341, 2347, 2351, 2357, 2371, 2377, 2381, 2383, 2389, 2393, 2399, 2411, 2417, 2423, 2437, 2441, 2447, 2459, 2467, 2473, 2477, 2503, 2521, 2531, 2539, 2543, 2549, 2551, 2557, 2579, 2591, 2593, 2609, 2617, 2621, 2633, 2647, 2657, 2659, 2663, 2671, 2677, 2683, 2687, 2689, 2693, 2699, 2707, 2711, 2713, 2719, 2729, 2731, 2741, 2749, 2753, 2767, 2777, 2789, 2791, 2797, 2801, 2803, 2819, 2833, 2837, 2843, 2851, 2857, 2861, 2879, 2887, 2897, 2903, 2909, 2917, 2927, 2939, 2953, 2957, 2963, 2969, 2971, 2999, 3001, 3011, 3019, 3023, 3037, 3041, 3049, 3061, 3067, 3079, 3083, 3089, 3109, 3119, 3121, 3137, 3163, 3167, 3169, 3181, 3187, 3191, 3203, 3209, 3217, 3221, 3229, 3251, 3253, 3257, 3259, 3271, 3299, 3301, 3307, 3313, 3319, 3323, 3329, 3331, 3343, 3347, 3359, 3361, 3371, 3373, 3389, 3391, 3407, 3413, 3433, 3449, 3457, 3461, 3463, 3467, 3469, 3491, 3499, 3511, 3517, 3527, 3529, 3533, 3539, 3541, 3547, 3557, 3559, 3571, 3581, 3583, 3593, 3607, 3613, 3617, 3623, 3631, 3637, 3643, 3659, 3671 };
    #endregion

    #region Functions
    public static Vector2Int ZToFraction(float Z)
    {
        float epsilon = Mathf.Pow(2, -20);

        Z = Mathf.Abs(Z);
        int n = Mathf.FloorToInt(Z);
        float x = Z - n;

        if (x < epsilon)
        {
            return new(n, 1);
        }
        else if (1 - x < epsilon)
        {
            return new(n + 1, 1);
        }

        int upn = 1;
        int upd = 1;
        int lon = 0;
        int lod = 1;
        int min, mid;

        int i = 0;

        while (i < 90000)
        {
            min = upn + lon;
            mid = upd + lod;

            //Debug.Log(min + "/" + mid + " => " + (mid * (x + (n * epsilon)) < min));
            if (mid * (x + (mid * epsilon)) < min)
            {
                upn = min;
                upd = mid;
                //Debug.Log("First check: " + lon + "/" + lod + " ; " + min + "/" + mid + " ; " + upn + "/" + upd);
            }
            else
            {
                //Debug.Log("Second phase");

                if (mid * (x - (mid * epsilon)) > min)
                {
                    lon = min;
                    lod = mid;
                    //Debug.Log("Second check: " + lon + "/" + lod + " ; " + min + "/" + mid + " ; " + upn + "/" + upd);
                }
                else
                {
                    //Debug.Log("Output check: " + lon + "/" + lod + " ; " + min + "/" + mid + " ; " + upn + "/" + upd);
                    i = 100000;
                    return new((mid * n) + min, mid);
                }
            }

            i++;
        }

        throw new System.Exception("Float to fraction conversion failure");
    }

    public static PFrac FloatToPFrac(float Z)
    {
        PFrac F = new();
        F.s = Z >= 0;

        if (Mathf.Approximately(Mathf.Floor(Z), Z))
        {
            F.n = NToFactors(Mathf.FloorToInt(Z));
            F.d = new() { 1 };
        }
        else
        {
            Vector2Int Frac = ZToFraction(Z);

            F.n = NToFactors(Frac.x);
            F.d = NToFactors(Frac.y);
        }

        return F;
    }

    public static void FloatToPFrac(float Z, out bool s, out List<byte> n, out List<byte> d)
    {
        s = Z >= 0;

        if (Mathf.Approximately(Mathf.Floor(Z), Z))
        {
            n = NToFactors(Mathf.FloorToInt(Z));
            d = new() { 1 };
        }
        else
        {
            Vector2Int Frac = ZToFraction(Z);

            n = NToFactors(Frac.x);
            d = NToFactors(Frac.y);
        }
    }

    public static float PFracToFloat(PFrac F)
    {
        F = +F;
        float Z;

        if (F.d.Count == 0) throw new System.Exception("Denominator can't be equal to zero");

        Z = F.s ? 1 : -1;
        Z *= FactorsToN(F.n);
        Z /= FactorsToN(F.d);

        return Z;
    }

    public static int BytePow(byte b, byte e)
    {
        int r = 1;
        for (int i = 0; i < e; i++) r *= b;
        return r;
    }

    public static int IntPow(int b, int e)
    {
        int r = 1;
        for (int i = 0; i < e; i++) r *= b;
        return r;
    }

    public static int Abs(int N) { return (N < 0) ? -N : N; }

    public static int FactorsToN(List<byte> P)
    {
        int N = (P.Count != 0) ? 1 : 0;

        if (P.Count != 1)
        {
            for (int i = 0; i < P.Count; i += 2)
            {
                N *= IntPow(PrimeList[P[i]], P[i + 1] + 1);
            }
        }

        return N;
    }

    public static List<byte> NToFactors(int N)
    {
        if (N == 0) return new() { };
        if (N == 1 || N == -1) return new() { 1 };

        List<byte> P = new() { };
        N = Abs(N);

        if (N != 0)
        {
            int i = 0;
            while (N != 1)
            {
                if (i > 255) throw new System.Exception("Prime factor(s) above supported list");

                if (N % PrimeList[i] == 0)
                {
                    P.Add((byte)i);
                    N /= PrimeList[i];
                    P.Add(0);

                    while (N % PrimeList[i] == 0)
                    {
                        N /= PrimeList[i];
                        P[P.Count - 1]++;
                    }
                }

                i++;
            }
        }

        return P;
    }

    public static List<byte> FacMCD(List<byte> a, List<byte> b)
    {
        if (a.Count == 0 || b.Count == 0) throw new System.Exception("MCD of 0 not possible");
        if (a.Count == 1 || b.Count == 1) return new() { 1 };

        List<byte> r = new() { };

        int ii = 0;
        for (int i = 0; i < a.Count && ii < b.Count; i+=2)
        {
            if (a[i] > b[ii])
            {
                ii += 2;
            }
            else if (a[i] == b[ii])
            {
                if (a[i + 1] != b[ii + 1])
                {
                    r.Add(a[i]);

                    if (a[i + 1] < b[ii + 1])
                        r.Add(a[i + 1]);
                    else
                        r.Add(b[i + 1]);
                }

                ii += 2;
            }
        }

        if (r.Count == 0) r.Add(1);

        return r;
    }

    public static List<byte> FacGCD(List<byte> a, List<byte> b) => FacMCD(a, b);

    public static List<byte> FacGCF(List<byte> a, List<byte> b) => FacMCD(a, b);

    public static List<byte> Facmcm(List<byte> a, List<byte> b)
    {
        if (a.Count == 0 || b.Count == 0) return new() { };
        if (a.Count == 1) return b;
        if (b.Count == 1) return a;

        List<byte> r = new() { };

        int ii = 0;
        for (int i = 0; i < a.Count && ii < b.Count; i += 2)
        {
            if (a[i] < b[ii])
            {
                r.Add(a[i]);
                r.Add(a[i + 1]);
            }
            else if (a[i] == b[ii])
            {
                r.Add(a[i]);

                if (a[i + 1] >= b[ii + 1])
                    r.Add(a[i + 1]);
                else
                    r.Add(b[ii + 1]);

                ii += 2;
            }
            else
            {
                r.Add(b[ii]);
                r.Add(b[ii + 1]);
                ii += 2;
            }
        }

        return r;
    }

    public static List<byte> FacLCM(List<byte> a, List<byte> b) => Facmcm(a, b);

    public static List<byte> FacSCM(List<byte> a, List<byte> b) => Facmcm(a, b);
    #endregion

    #region Static properties
    public static PFrac zero => new(new List<byte> { }, new List<byte> { 1 });

    public static PFrac plus1 => new(new List<byte> { 1 }, new List<byte> { 1 });

    public static PFrac minus1 => new(false, new List<byte> { 1 }, new List<byte> { 1 });

    public static PFrac infinity => new(new List<byte> { 1 }, new List<byte> { });

    public static PFrac undefined => new(new List<byte> { }, new List<byte> { });
    #endregion

    #region Static operators
    public static PFrac operator +(PFrac F)
    {
        if (F.n.Count == 1 || F.d.Count == 1) return F;
        if (F.n.Count == 0 && F.n.Count == 0) return undefined;
        if (F.n.Count == 0) return zero;
        if (F.d.Count == 0) return infinity;

        int ii = 0;
        for (int i = 0; i < F.n.Count && ii < F.d.Count; i += 2)
        {
            if (F.n[i] > F.d[ii])
            {
                ii += 2;
            }
            else if (F.n[i] == F.d[ii])
            {
                if (F.n[i + 1] == F.d[ii + 1])
                {
                    F.n.RemoveAt(i + 1);
                    F.n.RemoveAt(i);
                    F.d.RemoveAt(ii + 1);
                    F.d.RemoveAt(ii);
                }
                else if (F.n[i + 1] > F.d[ii + 1])
                {
                    F.n[i + 1] -= (byte)(F.d[ii + 1] + 1);
                    F.d.RemoveAt(ii + 1);
                    F.d.RemoveAt(ii);
                }
                else
                {
                    F.d[ii + 1] -= (byte)(F.n[i + 1] + 1);
                    F.n.RemoveAt(i + 1);
                    F.n.RemoveAt(i);
                }
            }
        }

        if (F.n.Count == 0) F.n = new() { 1 };
        if (F.d.Count == 0) F.d = new() { 1 };

        return F;
    }

    public static PFrac operator -(PFrac F) => +new PFrac(!F.s, F.n, F.d);

    public static PFrac operator *(PFrac left, PFrac right)
    {
        left = +left;
        right = +right;

        if (left.n.Count == 0 || right.n.Count == 0)
            return zero;

        if (left.d.Count == 0 && right.d.Count == 0)
            return infinity;
        else if (left.d.Count == 0 ^ right.d.Count == 0)
            return undefined;

        if (left == plus1) return right;
        if (right == plus1) return left;
        if (left == minus1) return -right;
        if (right == minus1) return -left;


        PFrac result = new();
        result.s = !(left.s ^ right.s);
        result.n = new() { };
        result.d = new() { };


        if (left.n.Count == 1)
            result.n = right.n;
        else if (right.n.Count == 1)
            result.n = left.n;
        else
        {
            int i = 0; int ii = 0;
            while (i < left.n.Count || ii < right.n.Count)
            {
                if (i < left.n.Count && (ii >= right.d.Count || left.n[i] < right.n[ii]))
                {
                    result.n.Add(left.n[i]);
                    result.n.Add(left.n[i + 1]);
                    i += 2;
                }
                else if (i < left.n.Count && ii < right.n.Count && left.n[i] == right.n[ii])
                {
                    result.n.Add(left.n[i]);
                    result.n.Add((byte)(left.n[i + 1] + right.n[ii + 1] + 1));
                    i += 2;
                    ii += 2;
                }
                else if (i >= left.n.Count || ii < right.n.Count)
                {
                    result.n.Add(right.n[ii]);
                    result.n.Add(right.n[ii + 1]);
                    ii += 2;
                }
            }
        }

        if (left.d.Count == 1)
            result.d = right.d;
        else if (right.d.Count == 1)
            result.d = left.d;
        else
        {
            int i = 0; int ii = 0;
            while (i < left.d.Count || ii < right.d.Count)
            {
                if (i < left.d.Count && (ii >= right.d.Count || left.d[i] < right.d[ii]))
                {
                    result.d.Add(left.d[i]);
                    result.d.Add(left.d[i + 1]);
                    i += 2;
                }
                else if (i < left.d.Count && ii < right.d.Count && left.d[i] == right.d[ii])
                {
                    result.d.Add(left.d[i]);
                    result.d.Add((byte)(left.d[i + 1] + right.d[ii + 1] + 1));
                    i += 2;
                    ii += 2;
                }
                else if (i >= left.d.Count || ii < right.d.Count)
                {
                    result.d.Add(right.d[ii]);
                    result.d.Add(right.d[ii + 1]);
                    ii += 2;
                }
            }
        }

        return +result;
    }

    public static PFrac operator /(PFrac left, PFrac right)
    {
        left = +left;
        right = +right;

        return +(left * new PFrac(right.s, right.d, right.n));
    }

    public static PFrac operator *(PFrac left, int right)
    {
        left = +left;

        return +(left * new PFrac(right >= 0, NToFactors(right), new() { 1 }));
    }

    public static PFrac operator *(int left, PFrac right) => right * left;

    public static PFrac operator /(PFrac left, int right)
    {
        left = +left;

        return +(left * new PFrac(right >= 0, new() { 1 }, NToFactors(right)));
    }

    //public static PFrac operator /(int left, PFrac right) => right / left;

    public static PFrac operator +(PFrac left, PFrac right)
    {
        left = +left;
        right = +right;

        if (left == right) return left * 2;
        if (left == -right) return zero;

        PFrac r = new();

        if (!(left.s ^ right.s))
        {
            r.s = left.s;
            r.d = Facmcm(left.d, right.d);

            r.n = NToFactors((FactorsToN(left.n) * FactorsToN(r.d) / FactorsToN(left.d)) + (FactorsToN(right.n) * FactorsToN(r.d) / FactorsToN(right.d)));
        }
        else
        {
            r.s = left > right;
            r.d = Facmcm(left.d, right.d);

            r.n = NToFactors((FactorsToN(left.n) * FactorsToN(r.d) / FactorsToN(left.d)) - (FactorsToN(right.n) * FactorsToN(r.d) / FactorsToN(right.d)));
        }

        return +r;
    }

    public static PFrac operator -(PFrac left, PFrac right) => left + (-right);

    public static bool operator >(PFrac left, PFrac right)
    {
        left = +left;
        right = +right;

        if (left.s)
        {
            if (!right.s) return true;

            if (left.d == right.d)
            {
                return FactorsToN(left.n) > FactorsToN(right.n);
            }
            else if (left.n == right.n)
            {
                return FactorsToN(left.d) < FactorsToN(right.d);
            }
            else
            {
                return PFracToFloat(left) > PFracToFloat(right);
            }
        }
        else
        {
            if (right.s) return false;

            if (left.d == right.d)
            {
                return FactorsToN(left.n) < FactorsToN(right.n);
            }
            else if (left.n == right.n)
            {
                return FactorsToN(left.d) > FactorsToN(right.d);
            }
            else
            {
                return PFracToFloat(left) > PFracToFloat(right);
            }
        }
        
    }

    public static bool operator <(PFrac left, PFrac right) => right > left;

    public static bool operator ==(PFrac left, PFrac right)
    {
        left = +left;
        right = +right;

        return left.s == right.s && left.n == right.n && left.d == right.d;
    } 

    public static bool operator !=(PFrac left, PFrac right) => !(left == right);

    public static explicit operator PFrac(float Z) => FloatToPFrac(Z);

    public static explicit operator PFrac(int N) => FloatToPFrac(N);
    #endregion

    #region Static overrides
    public override string ToString() => $"{(s ? '+' : '-')}{FactorsToN(n)}/{FactorsToN(d)}";

    public override bool Equals(object obj) => new PFrac(s, n, d) == (PFrac)obj;

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
    #endregion
}