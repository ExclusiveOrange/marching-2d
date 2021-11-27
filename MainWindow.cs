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
    private readonly Image image = new Bitmap(imageWidth, imageHeight, imageFormat);

    private const float xScale = 1f;
    private const float xOffset = 0f;
    private const float yScale = 1f;
    private const float yOffset = 0f;

    private const float rXScale = 1.0f / xScale;
    private const float rYScale = 1.0f / yScale;

    private readonly FastNoiseLite fastNoise = new FastNoiseLite();
    private const int seed = 1337;

    public
      MainWindow()
    {
      SetupUi();
      Render();
    }

    private
      void
      Render()
    {
      fastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
      fastNoise.SetSeed(seed);
      
      float noise(float x, float y) => fastNoise.GetNoise(x * rXScale + xOffset, y * rYScale + yOffset);

      Pen pen = new Pen(Color.White, 1);
      using (var graphics = Graphics.FromImage(image))
        MarchingRectangles.Draw(
          new MarchingRectangles.DrawParameters
          {
            pen = pen,
            graphics = graphics,
            imageWidth = imageWidth,
            imageHeight = imageHeight,
            gridWidth = 100,
            gridHeight = 100,
            noise = noise
          });
    }

    private
      void
      SetupUi()
    {
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(imageWidth, imageHeight);
      Text = @"Marching 2D";
      
      var pictureBox = new PictureBox();
      pictureBox.Image = image;
      pictureBox.Dock = DockStyle.Fill;
      pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
      Controls.Add(pictureBox);
    }
  }
}