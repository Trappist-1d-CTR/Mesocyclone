using System;
using System.Linq;
using MIConvexHull; // Install via NuGet: "MIConvexHull"

// Define a vertex class for 3D points
public class Vertex3 : IVertex
{
    public double[] Position { get; private set; }
    public bool IsList = false;
    public double Value; // Attribute (e.g., temperature)
    public double[] Values;

    public Vertex3()
    {
        Position = new[] { 0.0, 0.0, 0.0 };
        Value = 0;
        IsList = false;
    }

    public Vertex3(double x, double y, double z, double value)
    {
        Position = new[] { x, y, z };
        Value = value;
        IsList = false;
    }

    public Vertex3(double x, double y, double z, double[] values)
    {
        Position = new[] { x, y, z };
        Values = values;
        IsList = true;
    }
}

public static class NaturalNeighborInterpolation
{
    public static double[] Interpolate(Vertex3[] dataPoints, Vertex3 query)
    {
        if (dataPoints[0].IsList)
        {
            // Build Delaunay triangulation
            DelaunayTriangulation<Vertex3, DefaultTriangulationCell<Vertex3>> delaunay = DelaunayTriangulation<Vertex3, DefaultTriangulationCell<Vertex3>>.Create(dataPoints, 1e-10);

            // Find containing tetrahedron
            foreach (var cell in delaunay.Cells)
            {
                if (IsPointInTetrahedron(query.Position, cell))
                {
                    // Compute barycentric weights
                    double[] weights = BarycentricCoordinates(query.Position, cell);

                    double[] Results = new double[6];

                    // Interpolate attributes
                    for (int i = 0; i < 6; i++)
                    {
                        Results[i] = weights.Zip(cell.Vertices.Select(v => v.Values[i]), (w, v) => w * v).Sum();
                    }
                    return Results;
                }
            }

            // Outside convex hull → fallback (nearest neighbor)
            Vertex3 nearest = dataPoints.OrderBy(p => Distance(p.Position, query.Position)).First();
            return nearest.Values;
        }
        else
        {
            // Build Delaunay triangulation
            DelaunayTriangulation<Vertex3, DefaultTriangulationCell<Vertex3>> delaunay = DelaunayTriangulation<Vertex3, DefaultTriangulationCell<Vertex3>>.Create(dataPoints, 1e-10);

            // Find containing tetrahedron
            foreach (var cell in delaunay.Cells)
            {
                if (IsPointInTetrahedron(query.Position, cell))
                {
                    // Compute barycentric weights
                    double[] weights = BarycentricCoordinates(query.Position, cell);

                    // Interpolate attribute
                    return new double[1] { weights.Zip(cell.Vertices.Select(v => v.Value), (w, v) => w * v).Sum() };
                }
            }

            // Outside convex hull → fallback (nearest neighbor)
            Vertex3 nearest = dataPoints.OrderBy(p => Distance(p.Position, query.Position)).First();
            return new double[1] { nearest.Value };
        }
    }

    // Helper: check if point is inside tetrahedron
    private static bool IsPointInTetrahedron(double[] p, DefaultTriangulationCell<Vertex3> cell)
    {
        double[] w = BarycentricCoordinates(p, cell);
        return w.All(val => val >= -1e-9); // allow tiny epsilon
    }

    // Compute barycentric coordinates of point p in tetrahedron
    private static double[] BarycentricCoordinates(double[] p, DefaultTriangulationCell<Vertex3> cell)
    {
        double[][] v = cell.Vertices.Select(vv => vv.Position).ToArray();
        double detT = Determinant(v[0], v[1], v[2], v[3]);
        double w0 = Determinant(p, v[1], v[2], v[3]) / detT;
        double w1 = Determinant(v[0], p, v[2], v[3]) / detT;
        double w2 = Determinant(v[0], v[1], p, v[3]) / detT;
        double w3 = Determinant(v[0], v[1], v[2], p) / detT;
        return new[] { w0, w1, w2, w3 };
    }

    // Determinant for 4 points (signed volume *6)
    private static double Determinant(double[] a, double[] b, double[] c, double[] d)
    {
        double[,] m = {
            {a[0]-d[0], a[1]-d[1], a[2]-d[2]},
            {b[0]-d[0], b[1]-d[1], b[2]-d[2]},
            {c[0]-d[0], c[1]-d[1], c[2]-d[2]}
        };
        return m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1])
             - m[0, 1] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0])
             + m[0, 2] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);
    }

    private static double Distance(double[] a, double[] b)
    {
        double dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}

public class InterpolateAirCells
{
    public static double GetValue(Vertex3[] AirCells, Vertex3 PointPosition)
    {
        return NaturalNeighborInterpolation.Interpolate(AirCells, PointPosition)[0];
    }

    public static double[] GetValues(Vertex3[] AirCells, Vertex3 PointPosition)
    {
        return NaturalNeighborInterpolation.Interpolate(AirCells, PointPosition);
    }
}

// Example usage
public class Program
{
    public static double Main()
    {
        Vertex3[] points = new Vertex3[]
        {
            new Vertex3(0,0,0, 10),
            new Vertex3(5,0,0, 20),
            new Vertex3(0,3,0, 30),
            new Vertex3(0,0,7, 40),
            new Vertex3(-1,-3,0, 4)
        };

        Vertex3 query = new Vertex3(0.3, 0.3, 0.3, 0); // attribute ignored

        double interpolated = NaturalNeighborInterpolation.Interpolate(points, query)[0];

        return interpolated;
    }
}
