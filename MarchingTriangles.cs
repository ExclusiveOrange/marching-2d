using System;
using System.Drawing;

namespace marching_2d
{
  public class MarchingTriangles
  {
    public static
      void
      Draw(RenderParameters rp, int triangleSideLength, FieldFunction fieldFunction)
    {
      const float sqrt3 = 1.7320508075688772f;
      float triangleHeight = sqrt3 * triangleSideLength * 0.5f;
      int numTrianglesWideEvenRows = (int) Math.Ceiling(0.5f + (float) rp.imageWidth / triangleSideLength);
      int numTrianglesWideOddRows = (int) Math.Ceiling((float) rp.imageWidth / triangleSideLength);
      int numTrianglesHigh = (int) Math.Ceiling(rp.imageHeight / triangleHeight);

      void fillRow(float[] row, int iRow)
      {
        bool even = (iRow & 1) == 0;
        float x = even ? -0.5f * triangleSideLength : 0f;
        int numWide = even ? numTrianglesWideEvenRows : numTrianglesWideOddRows;

        for (int iX = 0; iX <= numWide; ++iX, x += triangleSideLength)
          row[iX] = fieldFunction(x, iRow * triangleHeight);
      }

      // 0 and 7 are ignored but it's faster to leave them in than to adjust bits
      //                       0  1  2  3  4  5  6  7
      int[] mapBitsToIndexA = {0, 0, 1, 2, 2, 1, 0, 0};
      int[] mapBitsToIndexB = {0, 1, 0, 0, 0, 0, 1, 0};
      int[] mapBitsToIndexC = {0, 2, 2, 1, 1, 2, 2, 0};

      float[] vals = new float[3];
      float[] xs = new float[3];
      float[] ys = new float[3];
      
      void renderLine(int vertexBits, float x, float y0, float y1)
      {
        (xs[0], xs[1], xs[2]) = (x, x + triangleSideLength, x + 0.5f * triangleSideLength);
        (ys[0], ys[1], ys[2]) = (y0, y0, y1);
        
        // one edge is from vals[iA] to vals[iB], the other from vals[iA] to vals[iC]
        var (ai, bi, ci) = (mapBitsToIndexA[vertexBits], mapBitsToIndexB[vertexBits], mapBitsToIndexC[vertexBits]);

        // swizzle values into the correct order
        var (ax, bx, cx) = (xs[ai], xs[bi], xs[ci]);
        var (ay, by, cy) = (ys[ai], ys[bi], ys[ci]);

        // calculate midpoints
        var (abt, act) = (zero(vals[ai], vals[bi]), zero(vals[ai], vals[ci]));
        var (abx, aby) = (lerp(ax, bx, abt), lerp(ay, by, abt));
        var (acx, acy) = (lerp(ax, cx, act), lerp(ay, cy, act));

        // grid
        if (rp.gridPen is not null)
        {
          rp.graphics.DrawLine(rp.gridPen, ax, ay, bx, by);
          rp.graphics.DrawLine(rp.gridPen, ax, ay, cx, cy);
          rp.graphics.DrawLine(rp.gridPen, bx, by, cx, cy);
        }

        // isopath
        rp.graphics.DrawLine(rp.isopathPen, abx, aby, acx, acy);
      }

      float[] rowEven = new float[numTrianglesWideEvenRows + 1];
      float[] rowOdd = new float[numTrianglesWideOddRows + 1];

      fillRow(rowEven, 0);

      //   numTrianglesWideEvenRows downward pointing triangles
      //   numTrianglesWideOddRows upward pointing triangles
      for (int iRow = 0; iRow < numTrianglesHigh; ++iRow)
      {
        bool even = (iRow & 1) == 0;

        fillRow(even ? rowOdd : rowEven, iRow + 1);

        float evenY = ((even ? 0 : 1) + iRow) * triangleHeight;
        float oddY = ((even ? 1 : 0) + iRow) * triangleHeight;

        // triangles where base is an even row
        float x = -0.5f * triangleSideLength;
        for (int xi = 0; xi < numTrianglesWideEvenRows; ++xi, x += triangleSideLength)
        {
          (vals[0], vals[1], vals[2]) = (rowEven[xi], rowEven[xi + 1], rowOdd[xi]);
          int vertexBits = (vals[0] > 0 ? 1 : 0) + (vals[1] > 0 ? 2 : 0) + (vals[2] > 0 ? 4 : 0);

          if (vertexBits is not 0 or 7)
            renderLine(vertexBits, x, evenY, oddY);
        }

        //triangles where base is an odd row
        x = 0f;
        for (int xi = 0; xi < numTrianglesWideOddRows; ++xi, x += triangleSideLength)
        {
          (vals[0], vals[1], vals[2]) = (rowOdd[xi], rowOdd[xi + 1], rowEven[xi + 1]);
          int vertexBits = (vals[0] > 0 ? 1 : 0) + (vals[1] > 0 ? 2 : 0) + (vals[2] > 0 ? 4 : 0);

          if (vertexBits is not 0 or 7)
            renderLine(vertexBits, x, oddY, evenY);
        }
      }
    }

    //==============================================================================================================================================================

    private static float lerp(float a, float b, float t) => a + t * (b - a);
    private static float zero(float a, float b) => 1.0f - b / (b - a);
  }
}