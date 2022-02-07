using System;

// using System.Drawing;
// using MathNet.Numerics.LinearAlgebra;
// using MathNet.Numerics.LinearAlgebra.Single;

namespace marching_2d
{
  internal class DualContouring2
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
      const float constrainStrength = 0.05f; // probably should be larger for larger grid cells and smaller for smaller grid cells

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

      // static float zero(float a, float b) => 1.0f - b / (b - a);
      static float zero(float a, float b) => a / (a - b);

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

      // Pen edgePen = new(Color.Peru, 1);

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
      var cellEdges = new (float x, float y, float nx, float ny, float xy_dot_nxny)[6];
      int numCellEdges;

      (float x, float y)
        estimateVertex()
      {
        // Vertex Estimation Algorithm
        // for Dual Contouring
        // Inspired by some forum post I read somewhere where the person claimed to follow the tangent lines.
        // I didn't see the code but the idea seems simple at least in two dimensions.
        //
        // Concept:
        //   estimate[0] is mean of edge intersections
        //   estimate[1] is mean of projections of estimate[0] on edge intersection tangent lines
        //   estimate[2] is mean of projections of estimate[1] on edge intersection tangent lines
        //   .. and so on until we get tired.
        //   Ideally this estimate gets better every iteration.
        //
        // Projections:
        //   For each edge intersection tangent line, the projection is the point on the line nearest the estimate
        //   from the previous step.
        //
        // Estimation:
        //   mean of q's
        //
        // Stopping condition:
        //   Currently stops after fixed number of iterations.
        //   Perhaps ideally it should stop after max number of iterations
        //   or when estimate[n] - estimate[n-1] is small.
        //
        // Bad behavior:
        //   Not sure. Presumably though the vertex could wander outside of the cell in the right conditions.
        //   One such condition is when there are two edge intersections for a cell, and their tangents intersect
        //   beyond the cell. This can happen when the field features are smaller than the size of the cell.
        //   In this case there is no ideal solution;
        //     either we let the vertex be outside its cell, which may put it in the correct place,
        //     or we clamp the vertex to the cell, which means it probably isn't following the isosurface.

        var mean = (x: 0f, y: 0f);

        // measure initial mean
        for (int iEdge = 0; iEdge < numCellEdges; ++iEdge)
          mean = (mean.x + cellEdges[iEdge].x, mean.y + cellEdges[iEdge].y);

        mean = (mean.x / numCellEdges, mean.y / numCellEdges);

        // refine mean
        for (int its = 10; its >= 0; --its)
        {
          var newMean = (x: 0f, y: 0f);

          // estimate new mean by projecting previous mean onto each edge tangent line at (x, y) + t(dx, dy)
          for (int iEdge = 0; iEdge < numCellEdges; ++iEdge)
          {
            // Calculate the point on the edge intersection tangent line closest to the current mean.
            // using a formula for a generalized nearest point to hyperplane
            // from Wikipedia: https://en.wikipedia.org/w/index.php?title=Distance_from_a_point_to_a_plane&oldid=1052628003
            //   x = y - [((y - p) dot a) / (a dot a)] * a
            // where x is unknown nearest point to hyperplane from arbitrary point y,
            // where p is a known point on the hyperplane and a is a known vector perpendicular to the plane (a normal)
            // where (x - p) dot a = 0
            //   and equivalently x dot a = p dot a = xy_dot_nxny
            // In this case we know length(a) == 1 so we can simplify to:
            //   x = y - (y dot a - d) * a
            // where "x", "y", "a" and "p" are the symbols from the wikipedia page; substituted below for appropriate variable names.
            var t = mean.x * cellEdges[iEdge].nx + mean.y * cellEdges[iEdge].ny - cellEdges[iEdge].xy_dot_nxny;
            var nearest_point_on_line = (x: mean.x - t * cellEdges[iEdge].nx, y: mean.y - t * cellEdges[iEdge].ny);
            newMean = (newMean.x + nearest_point_on_line.x, newMean.y + nearest_point_on_line.y);
          }

          // update refined mean
          mean = (newMean.x / numCellEdges, newMean.y / numCellEdges);
        }

        return mean;
      }

      void SetCellEdge(int edgeIndex, float x, float y, float nx, float ny)
      {
        cellEdges[edgeIndex] = (x, y, nx, ny, x * nx + y * ny);
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
              SetCellEdge(numCellEdges++, edgePositionsX[yi, x], ys[yi], nx, ny);
              if (rp.gridPen is not null)
                rp.graphics.DrawEllipse(rp.isopathPen, edgePositionsX[yi, x] - 1.5f, ys[yi] - 1.5f, 3, 3);
            }

          for (int xi = x; xi <= x + 1; ++xi)
            if (edgeExistencesY[y, xi])
            {
              var (nx, ny) = edgeNormalsY[y, xi];
              SetCellEdge(numCellEdges++, xs[xi], edgePositionsY[y, xi], nx, ny);
              if (rp.gridPen is not null)
                rp.graphics.DrawEllipse(rp.isopathPen, xs[xi] - 1.5f, edgePositionsY[y, xi] - 1.5f, 3, 3);
            }

          bool vertexExists = numCellEdges != 0;
          vertexExistences[y, x] = vertexExists;

          if (vertexExists)
          {
            vertexPositions[y, x] = estimateVertex();
            if (rp.gridPen is not null)
              rp.graphics.DrawEllipse(rp.isopathPen, vertexPositions[y, x].x - 1.5f, vertexPositions[y, x].y - 1.5f, 3, 3);
          }
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