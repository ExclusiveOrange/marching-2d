using System;
using System.Drawing;

namespace marching_2d
{
  internal class MarchingRectangles
  {
    public struct Parameters
    {
      public int gridWidth;
      public int gridHeight;
      public FieldFunction fieldFunction;
    }

    public static
      void
      Draw(RenderParameters rp, Parameters p)
    {
      Func<CornerValues, (PointF pt1, PointF pt2)?>[] valuesToLineFuncs = createValuesToLineFuncs();

      void fillRow(float[] row, int y)
      {
        for (int x = 0; x <= p.gridWidth; ++x)
          row[x] = p.fieldFunction(x * (float) rp.imageWidth / p.gridWidth, y * (float) rp.imageHeight / p.gridHeight);
      }

      Point localToImage(int gridX, int gridY, PointF p) => new() {X = localXToImage(gridX, p.X), Y = localYToImage(gridY, p.Y)};
      int localXToImage(int gridX, float cellX) => (int) ((gridX + cellX) * rp.imageWidth / p.gridWidth);
      int localYToImage(int gridY, float cellY) => (int) ((gridY + cellY) * rp.imageHeight / p.gridHeight);
      (PointF pt1, PointF pt2)? tryGetLocalLine(CornerValues cornerValues) => valuesToLineFuncs[getCellFuncIndex(cornerValues)](cornerValues);

      // Row Buffers
      float[] rowA = new float[p.gridWidth + 1];
      float[] rowB = new float[p.gridWidth + 1];

      fillRow(rowB, 0);
      
      for (int y = 0; y < p.gridHeight; ++y)
      {
        (rowB, rowA) = (rowA, rowB);
        fillRow(rowB, y + 1);

        for (int x = 0; x < p.gridWidth; ++x)
        {
          var cornerValues = new CornerValues {tl = rowA[x], tr = rowA[x + 1], bl = rowB[x], br = rowB[x + 1]};
          if (tryGetLocalLine(cornerValues) is (PointF, PointF) localLine)
          {
            rp.graphics.DrawLine(rp.gridPen, localToImage(x, y, new PointF(0, 0)), localToImage(x, y, new PointF(1, 0)));
            rp.graphics.DrawLine(rp.gridPen, localToImage(x, y, new PointF(0, 0)), localToImage(x, y, new PointF(0, 1)));
            rp.graphics.DrawLine(rp.gridPen, localToImage(x, y, new PointF(1, 1)), localToImage(x, y, new PointF(1, 0)));
            rp.graphics.DrawLine(rp.gridPen, localToImage(x, y, new PointF(1, 1)), localToImage(x, y, new PointF(0, 1)));
            
            rp.graphics.DrawLine(rp.isopathPen, localToImage(x, y, localLine.pt1), localToImage(x, y, localLine.pt2));
          }
        }
      }
    }

    //==============================================================================================================================================================
    
    private struct CornerValues { public float tl, tr, bl, br; }

    private static
      Func<CornerValues, (PointF pt1, PointF pt2)?>[]
      createValuesToLineFuncs()
    {
      var funcs = new Func<CornerValues, (PointF pt1, PointF pt2)?>[16];

      for (int i = 0; i < 16; ++i)
        funcs[i] = (vs) => null;

      // small corners
      funcs[1] = vs => (new PointF(0f, zero(vs.tl, vs.bl)), new PointF(zero(vs.tl, vs.tr), 0f));
      funcs[2] = vs => (new PointF(zero(vs.tl, vs.tr), 0f), new PointF(1f, zero(vs.tr, vs.br)));
      funcs[4] = vs => (new PointF(0f, zero(vs.tl, vs.bl)), new PointF(zero(vs.bl, vs.br), 1f));
      funcs[8] = vs => (new PointF(zero(vs.bl, vs.br), 1f), new PointF(1f, zero(vs.tr, vs.br)));

      // cardinals
      funcs[3] = vs => (new PointF(0f, zero(vs.tl, vs.bl)), new PointF(1f, zero(vs.tr, vs.br)));
      funcs[5] = vs => (new PointF(zero(vs.tl, vs.tr), 0f), new PointF(zero(vs.bl, vs.br), 1f));
      funcs[10] = funcs[5];
      funcs[12] = funcs[3];

      // diagonals: TODO: figure out how to render these since they are impossible with only one line
      // funcs[6] = (vs) => (new PointF(0f, 1f), new PointF(1f, 0f));
      // funcs[9] = (vs) => (new PointF(0f, 0f), new PointF(1f, 1f));

      // big corners
      funcs[7] = vs => (new PointF(zero(vs.bl, vs.br), 1f), new PointF(1f, zero(vs.tr, vs.br)));
      funcs[11] = vs => (new PointF(0f, zero(vs.tl, vs.bl)), new PointF(zero(vs.bl, vs.br), 1f));
      funcs[13] = vs => (new PointF(zero(vs.tl, vs.tr), 0f), new PointF(1f, zero(vs.tr, vs.br)));
      funcs[14] = vs => (new PointF(0f, zero(vs.tl, vs.bl)), new PointF(zero(vs.tl, vs.tr), 0f));

      return funcs;
    }

    private static int getCellFuncIndex(CornerValues values) => (values.tl > 0 ? 1 : 0) + (values.tr > 0 ? 2 : 0) + (values.bl > 0 ? 4 : 0) + (values.br > 0 ? 8 : 0);
    private static float zero(float a, float b) => 1.0f - b / (b - a);
  }
}