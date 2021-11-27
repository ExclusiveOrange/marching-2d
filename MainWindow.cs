using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace marching_2d
{
  public sealed class MainWindow : Form
  {
    private const int imageWidth = 1000;
    private const int imageHeight = 1000;
    private const PixelFormat imageFormat = PixelFormat.Format32bppRgb;
    private readonly Bitmap imageRectangles = new Bitmap(imageWidth, imageHeight, imageFormat);
    private readonly Bitmap imageTriangles = new Bitmap(imageWidth, imageHeight, imageFormat);

    private const float xScale = 5f;
    private const float xOffset = 0f;
    private const float yScale = 5f;
    private const float yOffset = 0f;

    private const float rXScale = 1.0f / xScale;
    private const float rYScale = 1.0f / yScale;

    private readonly FastNoiseLite fastNoise = new FastNoiseLite();
    private const int seed = 1337;

    private static
      float
      clamp(float value, float min, float max)
    {
      return value < min ? min : value > max ? max : value;
    }

    public
      MainWindow()
    {
      SetupUi();

      fastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
      fastNoise.SetSeed(seed);
      
      for (int y = 0; y < imageHeight; ++y)
        for (int x = 0; x < imageWidth; ++x)
        {
          float noise = GetNoise(x, y);
          float snoise = clamp(0.5f + 5f * noise, 0f, 1f);
          int bnoise = (int)(255f * snoise);
          imageRectangles.SetPixel(x, y, Color.FromArgb(bnoise, bnoise, bnoise));
          imageTriangles.SetPixel(x, y, Color.FromArgb(bnoise, bnoise, bnoise));
        }

      Pen pen = new Pen(Color.Fuchsia, 2);
      
      using (var graphics = Graphics.FromImage(imageRectangles))
        RenderRectangles(pen, graphics);

      using (var graphics = Graphics.FromImage(imageTriangles))
        RenderTriangles(pen, graphics);
    }

    private
      float
      GetNoise(float x, float y) => fastNoise.GetNoise(x * rXScale + xOffset, y * rYScale + yOffset);

    private
      void
      RenderRectangles(Pen pen, Graphics graphics)
    {
      MarchingRectangles.Draw(
        new MarchingRectangles.DrawParameters
        {
          pen = pen,
          graphics = graphics,
          imageWidth = imageWidth,
          imageHeight = imageHeight,
          gridWidth = 100,
          gridHeight = 100,
          noise = GetNoise
        });
    }

    private
      void
      RenderTriangles(Pen pen, Graphics graphics)
    {
      MarchingTriangles.Draw(
        new MarchingTriangles.DrawParameters
        {
          pen = pen,
          graphics = graphics,
          imageWidth = imageWidth,
          imageHeight = imageHeight,
          triangleSideLength = 10,
          noise = GetNoise
        });
    }

    private
      void
      SetupUi()
    {
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(imageWidth * 2, imageHeight);
      FormBorderStyle = FormBorderStyle.FixedSingle;
      Text = @"Marching 2D";

      var pictureBoxRectangles = new PictureBox();
      pictureBoxRectangles.Size = new Size(imageWidth, imageHeight);
      pictureBoxRectangles.Image = imageRectangles;
      pictureBoxRectangles.Dock = DockStyle.Left;
      // pictureBoxRectangles.Anchor = AnchorStyles.Left | AnchorStyles.Top;
      pictureBoxRectangles.SizeMode = PictureBoxSizeMode.Zoom;
      Controls.Add(pictureBoxRectangles);
      
      var pictureBoxTriangles = new PictureBox();
      pictureBoxTriangles.Size = new Size(imageWidth, imageHeight);
      pictureBoxTriangles.Image = imageTriangles;
      pictureBoxTriangles.Dock = DockStyle.Right;
      // pictureBoxTriangles.Anchor = AnchorStyles.Right | AnchorStyles.Top;
      pictureBoxTriangles.SizeMode = PictureBoxSizeMode.Zoom;
      Controls.Add(pictureBoxTriangles);
    }
  }
}