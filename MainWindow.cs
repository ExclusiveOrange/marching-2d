using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace marching_2d
{
  internal struct CornerValues
  {
    public float tl, tr, bl, br;
  }

  public sealed class MainWindow : Form
  {
    private static int imageWidth = 1000;
    private static int imageHeight = 1000;
    private static PixelFormat imageFormat = PixelFormat.Format32bppRgb;

    private static int gridWidth = 1000;
    private static int gridHeight = 1000;
    private static float xScale = 1f;
    private static float xOffset = 0f;
    private static float yScale = 1f;
    private static float yOffset = 0f;

    private static float noiseXFactor = imageWidth / (gridWidth * xScale);
    private static float noiseYFactor = imageHeight / (gridHeight * yScale);

    private readonly Func<CornerValues, (PointF pt1, PointF pt2)?>[] valuesToLineFuncs;

    private readonly FastNoiseLite fastNoise = new FastNoiseLite();
    private static int seed = 1337;

    public
      MainWindow()
    {
      valuesToLineFuncs = createValuesToLineFuncs();

      fastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
      fastNoise.SetSeed(seed);

      AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      ClientSize = new System.Drawing.Size(imageWidth, imageHeight);
      Text = @"Marching Squares";

      // PictureBox
      var image = new Bitmap(imageWidth, imageHeight, imageFormat);
      var pictureBox = new PictureBox();
      pictureBox.Image = image;
      pictureBox.Dock = DockStyle.Fill;
      pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
      Controls.Add(pictureBox);

      // Helpers

      float noiseX(int x) => xOffset + x * noiseXFactor;
      float noiseY(int y) => yOffset + y * noiseYFactor;

      void fillRow(float[] row, int y)
      {
        float ny = noiseY(y);
        for (int x = 0; x <= gridWidth; ++x)
          row[x] = fastNoise.GetNoise(noiseX(x), ny);
      }

      // Row Buffers
      float[] rowA = new float[gridWidth + 1];
      float[] rowB = new float[gridWidth + 1];

      fillRow(rowA, 0);

      Pen pen = new Pen(Color.White, 1);

      using (var graphics = Graphics.FromImage(image))
        for (int y = 0; y < gridHeight; ++y)
        {
          if (y > 0)
            (rowB, rowA) = (rowA, rowB);

          fillRow(rowB, y);

          for (int x = 0; x < gridWidth; ++x)
          {
            var cellValues = new CornerValues {tl = rowA[x], tr = rowA[x + 1], bl = rowB[x], br = rowB[x + 1]};
            if (tryGetLocalLine(cellValues) is (PointF, PointF) localLine)
            {
              var pt1 = localToImage(x, y, localLine.pt1);
              var pt2 = localToImage(x, y, localLine.pt2);
              graphics.DrawLine(pen, pt1, pt2);
            }
          }
        }
    }

    private static float zero(float a, float b) => 1.0f - b / (b - a);

    private Func<CornerValues, (PointF pt1, PointF pt2)?>[]
      createValuesToLineFuncs()
    {
      var funcs = new Func<CornerValues, (PointF pt1, PointF pt2)?>[16];

      for (int i = 0; i < 16; ++i)
        funcs[i] = (vs) => null;

      // small corners
      funcs[1] = (vs) => (new PointF(0f, zero(vs.tl, vs.bl)), new PointF(zero(vs.tl, vs.tr), 0f));
      funcs[2] = (vs) => (new PointF(zero(vs.tl, vs.tr), 0f), new PointF(1f, zero(vs.tr, vs.br)));
      funcs[4] = (vs) => (new PointF(0f, zero(vs.tl, vs.bl)), new PointF(zero(vs.bl, vs.br), 1f));
      funcs[8] = (vs) => (new PointF(zero(vs.bl, vs.br), 1f), new PointF(1f, zero(vs.tr, vs.br)));

      // cardinals
      funcs[3] = (vs) => (new PointF(0f, zero(vs.tl, vs.bl)), new PointF(1f, zero(vs.tr, vs.br)));
      funcs[5] = (vs) => (new PointF(zero(vs.tl, vs.tr), 0f), new PointF(zero(vs.bl, vs.br), 1f));
      funcs[10] = funcs[5];
      funcs[12] = funcs[3];

      // diagonals: TODO: figure out how to render these since they are impossible with only one line
      // funcs[6] = (vs) => (new PointF(0f, 1f), new PointF(1f, 0f));
      // funcs[9] = (vs) => (new PointF(0f, 0f), new PointF(1f, 1f));

      // big corners
      funcs[7] = (vs) => (new PointF(zero(vs.bl, vs.br), 1f), new PointF(1f, zero(vs.tr, vs.br)));
      funcs[11] = (vs) => (new PointF(0f, zero(vs.tl, vs.bl)), new PointF(zero(vs.bl, vs.br), 1f));
      funcs[13] = (vs) => (new PointF(zero(vs.tl, vs.tr), 0f), new PointF(1f, zero(vs.tr, vs.br)));
      funcs[14] = (vs) => (new PointF(0f, zero(vs.tl, vs.bl)), new PointF(zero(vs.tl, vs.tr), 0f));

      return funcs;
    }

    private int
      getCellFuncIndex(CornerValues values)
      => (values.tl > 0 ? 1 : 0) + (values.tr > 0 ? 2 : 0) + (values.bl > 0 ? 4 : 0) + (values.br > 0 ? 8 : 0);

    private Point
      localToImage(int gridX, int gridY, PointF p) => new() {X = localXToImage(gridX, p.X), Y = localYToImage(gridY, p.Y)};

    private int
      localXToImage(int gridX, float cellX) => (int) ((gridX + cellX) * imageWidth / gridWidth);

    private int
      localYToImage(int gridY, float cellY) => (int) ((gridY + cellY) * imageHeight / gridHeight);

    private (PointF pt1, PointF pt2)?
      tryGetLocalLine(CornerValues cornerValues)
    {
      var funcIndex = getCellFuncIndex(cornerValues);
      var lineFunc = valuesToLineFuncs[funcIndex];
      return lineFunc(cornerValues);
    }
  }
}