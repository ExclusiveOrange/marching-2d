using System;
using System.Drawing;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace marching_2d
{
  internal class DualContouring
  {
    // Written by me, Atlee Brink, based on the description of the algorithm at
    // https://www.boristhebrave.com/2018/04/15/dual-contouring-tutorial/
    // and using the constraint trick from
    // https://github.com/BorisTheBrave/mc-dc/blob/a165b326849d8814fb03c963ad33a9faf6cc6dea/qef.py#L87
    // and using the pseudoinverse trick from
    // https://stackoverflow.com/a/31188308
    //
    // No copy-pasting was done.
    //
    // Thanks to Math.NET for their Matrix.PseudoInverse() implementation, used here to minimize the error
    // function representing the distance of an unknown vertex from multiple lines on the intersections of the isopath
    // with the grid cell edges and perpendicular to the field gradients at those intersection points.
    // see: https://numerics.mathdotnet.com/
    //
    // WARNING: this implementation is purely a discovery to see how well this algorithm works.
    // IT IS NOT OPTIMIZED FOR PERFORMANCE IN THE SLIGHTEST

    public static
      void
      Draw(RenderParameters rp, int gridWidth, int gridHeight, FieldFunction fieldFunction)
    {
      const bool constrain = true;
      const float constrainStrength = 0.05f; // discovered experimentally: 0.01f might work

      (float nx, float ny)
        normalAt(float x, float y)
      {
        const float small = 1f;

        float
          yn = fieldFunction(x, y - small),
          xn = fieldFunction(x - small, y),
          xp = fieldFunction(x + small, y),
          yp = fieldFunction(x, y + small);

        float
          xd = xp - xn,
          yd = yp - yn;

        float l = (float) Math.Sqrt(xd * xd + yd * yd);

        return l == 0f ? (1f, 0f) : (xd / l, yd / l);
      }

      static float zero(float a, float b) => 1.0f - b / (b - a);

      var cellWidth = rp.imageWidth / gridWidth;
      var cellHeight = rp.imageHeight / gridHeight;

      var xs = new float[gridWidth + 1];
      for (int x = 0; x <= gridWidth; ++x)
        xs[x] = x * cellWidth;

      var ys = new float[gridHeight + 1];
      for (int y = 0; y <= gridHeight; ++y)
        ys[y] = y * cellHeight;

      var corners = new float[gridHeight + 1, gridWidth + 1];
      for (int y = 0; y <= gridHeight; ++y)
        for (int x = 0; x <= gridWidth; ++x)
          corners[y, x] = fieldFunction(xs[x], ys[y]);

      var edgeExistencesX = new bool[gridHeight + 1, gridWidth];
      var edgePositionsX = new float[gridHeight + 1, gridWidth];
      var edgeNormalsX = new (float x, float y)[gridHeight + 1, gridWidth];
      for (int y = 0; y <= gridHeight; ++y)
        for (int x = 0; x < gridWidth; ++x)
        {
          float n = corners[y, x], p = corners[y, x + 1];
          bool e = (n > 0) != (p > 0);
          edgeExistencesX[y, x] = e;
          if (e)
          {
            var posX = xs[x] + cellWidth * zero(n, p);
            edgePositionsX[y, x] = posX;
            edgeNormalsX[y, x] = normalAt(posX, ys[y]);
          }
        }

      Pen edgePen = new(Color.Peru, 1);

      // draw intersection points
      // for (int y = 0; y <= gridHeight; ++y)
      //   for (int x = 0; x < gridWidth; ++x)
      //     if (edgeExistencesX[y, x])
      //     {
      //       const float ns = 20f;
      //       const float dotSize = 3f;
      //       var (posX, posY) = (edgePositionsX[y, x], ys[y]);
      //       rp.graphics.DrawEllipse(edgePen, posX - dotSize / 2f, posY - dotSize / 2f, dotSize, dotSize);
      //       var (nx, ny) = edgeNormalsX[y, x];
      //       rp.graphics.DrawLine(edgePen, posX, posY, posX + ns * nx, posY + ns * ny);
      //     }

      var edgeExistencesY = new bool[gridHeight, gridWidth + 1];
      var edgePositionsY = new float[gridHeight, gridWidth + 1];
      var edgeNormalsY = new (float x, float y)[gridHeight, gridWidth + 1];
      for (int y = 0; y < gridHeight; ++y)
        for (int x = 0; x <= gridWidth; ++x)
        {
          float n = corners[y, x], p = corners[y + 1, x];
          bool e = (n > 0) != (p > 0);
          edgeExistencesY[y, x] = e;
          if (e)
          {
            var posY = ys[y] + cellHeight * zero(n, p);
            edgePositionsY[y, x] = posY;
            edgeNormalsY[y, x] = normalAt(xs[x], posY);
          }
        }

      // draw intersection points
      // for (int y = 0; y < gridHeight; ++y)
      //   for (int x = 0; x <= gridWidth; ++x)
      //     if (edgeExistencesY[y, x])
      //     {
      //       const float ns = 20f;
      //       const float dotSize = 3f;
      //       var (posX, posY) = (xs[x], edgePositionsY[y, x]);
      //       rp.graphics.DrawEllipse(edgePen, posX - dotSize / 2f, posY - dotSize / 2f, dotSize, dotSize);
      //       var (nx, ny) = edgeNormalsY[y, x];
      //       rp.graphics.DrawLine(edgePen, posX, posY, posX + ns * nx, posY + ns * ny);
      //     }

      // draw grid but only where edges are crossed
      if (rp.gridPen is not null)
        for (int y = 0; y < gridHeight; ++y)
          for (int x = 0; x < gridWidth; ++x)
          {
            if (x < gridWidth && edgeExistencesX[y, x])
              rp.graphics.DrawLine(rp.gridPen, xs[x], ys[y], xs[x + 1], ys[y]);
            if (y < gridHeight && edgeExistencesY[y, x])
              rp.graphics.DrawLine(rp.gridPen, xs[x], ys[y], xs[x], ys[y + 1]);
          }

      var vertexExistences = new bool[gridHeight, gridWidth];
      var vertexPositions = new (float x, float y)[gridHeight, gridWidth];
      var cellEdges = new (float x, float y, float nx, float ny)[6];
      int numCellEdges;

      const int minNumCellEdges = constrain ? 2 : 0;

      (float x, float y)
        estimateVertex()
      {
        // thanks for pseudo-inverse trick: https://stackoverflow.com/a/31188308
        // thanks for constraint bias trick
        // https://www.boristhebrave.com/2018/04/15/dual-contouring-tutorial/
        // https://github.com/BorisTheBrave/mc-dc/blob/a165b326849d8814fb03c963ad33a9faf6cc6dea/qef.py#L87

        // calculate mean
        float meanx, meany;
        {
          float sumxs = 0f, sumys = 0f;
          for (int iRow = 0; iRow < numCellEdges; ++iRow)
          {
            var (x, y, _, _) = cellEdges[iRow];
            sumxs += x;
            sumys += y;
          }

          meanx = sumxs / numCellEdges;
          meany = sumys / numCellEdges;
        }

        if (constrain)
        {
          cellEdges[numCellEdges++] = (meanx, meany, constrainStrength, 0f);
          cellEdges[numCellEdges++] = (meanx, meany, 0f, constrainStrength);
        }

        Matrix<float> A = new DenseMatrix(numCellEdges, 2);
        for (int iRow = 0; iRow < numCellEdges; ++iRow)
        {
          var (_, _, nx, ny) = cellEdges[iRow];
          A[iRow, 0] = nx;
          A[iRow, 1] = ny;
        }

        Vector<float> b = new DenseVector(numCellEdges);
        for (int iRow = 0; iRow < numCellEdges; ++iRow)
        {
          var (x, y, nx, ny) = cellEdges[iRow];
          b[iRow] = x * nx + y * ny;
        }

        var pseudo = A.PseudoInverse();
        var leastSquares = pseudo.Multiply(b);

        return (leastSquares[0], leastSquares[1]);
      }

      // locate vertices within grid cells
      for (int y = 0; y < gridHeight; ++y)
        for (int x = 0; x < gridWidth; ++x)
        {
          numCellEdges = 0;

          for (int yi = y; yi <= y + 1; ++yi)
            if (edgeExistencesX[yi, x])
            {
              var (nx, ny) = edgeNormalsX[yi, x];
              cellEdges[numCellEdges++] = (edgePositionsX[yi, x], ys[yi], nx, ny);
            }

          for (int xi = x; xi <= x + 1; ++xi)
            if (edgeExistencesY[y, xi])
            {
              var (nx, ny) = edgeNormalsY[y, xi];
              cellEdges[numCellEdges++] = (xs[xi], edgePositionsY[y, xi], nx, ny);
            }

          bool vertexExists = numCellEdges != 0;
          vertexExistences[y, x] = vertexExists;

          if (vertexExists)
            vertexPositions[y, x] = estimateVertex();
        }

      // draw isopath to image
      // edges x
      for (int y = 1; y < gridHeight; ++y)
        for (int x = 0; x < gridWidth; ++x)
          if (edgeExistencesX[y, x])
          {
            var (vx0, vy0) = vertexPositions[y - 1, x];
            var (vx1, vy1) = vertexPositions[y, x];
            rp.graphics.DrawLine(rp.isopathPen, vx0, vy0, vx1, vy1);
          }

      // edges y
      for (int y = 0; y < gridHeight; ++y)
        for (int x = 1; x < gridWidth; ++x)
          if (edgeExistencesY[y, x])
          {
            var (vx0, vy0) = vertexPositions[y, x - 1];
            var (vx1, vy1) = vertexPositions[y, x];
            rp.graphics.DrawLine(rp.isopathPen, vx0, vy0, vx1, vy1);
          }
    }
  }
}