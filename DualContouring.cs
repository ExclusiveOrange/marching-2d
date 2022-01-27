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
        
        float l = (float)Math.Sqrt(xd * xd + yd * yd);
        
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
      
      // DELETE
      // draw intersection points
      for (int y = 0; y <= gridHeight; ++y)
        for (int x = 0; x < gridWidth; ++x)
          if (edgeExistencesX[y, x])
          {
            const float ns = 20f;
            const float dotSize = 3f;
            var (posX, posY) = (edgePositionsX[y, x], ys[y]);
            rp.graphics.DrawEllipse(rp.isopathPen, posX - dotSize / 2f, posY - dotSize / 2f, dotSize, dotSize);
            var (nx, ny) = edgeNormalsX[y, x];
            rp.graphics.DrawLine(rp.isopathPen, posX, posY, posX + ns * nx, posY + ns * ny);
          }

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
      
      // DELETE
      // draw intersection points
      for (int y = 0; y < gridHeight; ++y)
        for (int x = 0; x <= gridWidth; ++x)
          if (edgeExistencesY[y, x])
          {
            const float ns = 20f;
            const float dotSize = 3f;
            var (posX, posY) = (xs[x], edgePositionsY[y, x]);
            rp.graphics.DrawEllipse(rp.isopathPen, posX - dotSize / 2f, posY - dotSize / 2f, dotSize, dotSize);
            var (nx, ny) = edgeNormalsY[y, x];
            rp.graphics.DrawLine(rp.isopathPen, posX, posY, posX + ns * nx, posY + ns * ny);
          }

      // TODO: calculate field normals (must be normalized) at edge intersections
    }
  }
}