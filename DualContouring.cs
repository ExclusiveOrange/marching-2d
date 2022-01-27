using System;
using System.Drawing;

namespace marching_2d
{
  internal class DualContouring
  {
    public static
      void
      Draw(RenderParameters rp, int gridWidth, int gridHeight, FieldFunction fieldFunction)
    {
      float zero(float a, float b) => 1.0f - b / (b - a);
      
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
      for (int y = 0; y <= gridHeight; ++y)
        for (int x = 0; x < gridWidth; ++x)
        {
          float n = corners[y, x], p = corners[y, x + 1];
          bool e = (n > 0) != (p > 0);
          edgeExistencesX[y, x] = e;
          if (e)
            edgePositionsX[y, x] = xs[x] + cellWidth * zero(n, p);
        }
      
      // DELETE
      // draw intersection points
      for (int y = 0; y <= gridHeight; ++y)
        for (int x = 0; x < gridWidth; ++x)
          if (edgeExistencesX[y, x])
            rp.graphics.DrawEllipse(rp.isopathPen, edgePositionsX[y, x], ys[y], 3f, 3f);
      
      var edgeExistencesY = new bool[gridHeight, gridWidth + 1];
      var edgePositionsY = new float[gridHeight, gridWidth + 1];
      for (int y = 0; y < gridHeight; ++y)
        for (int x = 0; x <= gridWidth; ++x)
        {
          float n = corners[y, x], p = corners[y + 1, x];
          bool e = (n > 0) != (p > 0);
          edgeExistencesY[y, x] = e;
          if (e)
            edgePositionsY[y, x] = ys[y] + cellHeight * zero(n, p);
        }
      
      // DELETE
      // draw intersection points
      for (int y = 0; y < gridHeight; ++y)
        for (int x = 0; x <= gridWidth; ++x)
          if (edgeExistencesY[y, x])
            rp.graphics.DrawEllipse(rp.isopathPen, xs[x], edgePositionsY[y, x], 3f, 3f);

      // TODO: calculate field normals (must be normalized) at edge intersections
    }
  }
}