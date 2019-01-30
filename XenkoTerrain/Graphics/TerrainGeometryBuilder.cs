﻿using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Extensions;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;
using Xenko.Rendering;
using XenkoTerrain.Extensions;

namespace XenkoTerrain.Graphics
{
  // TODO: Extract interface, separate into water and terrain geometry builders
  public class TerrainGeometryBuilder
  {
    public const float MaxPixelColor = 256 * 256 * 256; // 3 color channels with 256 vals
    public const float HalfMaxPixelColor = MaxPixelColor / 2;

    private PixelBuffer heightMapPixelBuffer;

    public TerrainGeometryBuilder(GraphicsDevice graphicsDevice, float size, Image heightMapImage, float maxHeight)
    {
      heightMapPixelBuffer = heightMapImage.PixelBuffer[0];
      GraphicsDevice = graphicsDevice;
      Size = size;
      HeightMapImage = heightMapImage;
      MaxHeight = maxHeight;
    }

    public GraphicsDevice GraphicsDevice { get; }

    public float Size { get; }

    public Image HeightMapImage { get; }

    public float MaxHeight { get; }

    public GeometricPrimitive BuildTerrain()
    {
      var data = GenerateTerrainGeometry(HeightMapImage.Description.Width, HeightMapImage.Description.Height, Size, false);
      return new GeometricPrimitive(GraphicsDevice, data);
    }

    protected virtual GeometricMeshData<VertexPositionNormalTexture> GenerateTerrainGeometry(int tessellationX, int tessellationY, float size, bool generateBackFace)
    {
      var lineWidth = (tessellationX + 1);
      var lineHeight = (tessellationY + 1);
      var vertices = new VertexPositionNormalTexture[lineWidth * lineHeight * (generateBackFace ? 2 : 1)];
      var indices = new int[tessellationX * tessellationY * 6 * (generateBackFace ? 2 : 1)];
      var deltaX = size / tessellationX;
      var deltaY = size / tessellationY;

      size /= 2.0f;

      var vertexCount = 0;
      var indexCount = 0;

      // Create vertices
      var uv = new Vector2(1f, 1f);

      for (var y = 0; y < (tessellationY + 1); y++)
      {
        for (var x = 0; x < (tessellationX + 1); x++)
        {
          var height = x == 0 || y == 0 ? 0.1f : GetHeight(x, y);
          var position = new Vector3(-size + deltaX * x, height, -size + deltaY * y);
          var normal = GetNormal(x, y);
          var texCoord = new Vector2(uv.X * x / tessellationX, uv.Y * y / tessellationY);
          vertices[vertexCount++] = new VertexPositionNormalTexture(position, normal, texCoord);
        }
      }

      // Create indices
      for (var y = 0; y < tessellationY; y++)
      {
        for (var x = 0; x < tessellationX; x++)
        {
          // Six indices (two triangles) per face.
          var vbase = lineWidth * y + x;
          indices[indexCount++] = (vbase + 1);
          indices[indexCount++] = (vbase + 1 + lineWidth);
          indices[indexCount++] = (vbase + lineWidth);
          indices[indexCount++] = (vbase + 1);
          indices[indexCount++] = (vbase + lineWidth);
          indices[indexCount++] = (vbase);
        }
      }

      if (generateBackFace)
      {
        var numVertices = lineWidth * lineHeight;

        for (var y = 0; y < (tessellationY + 1); y++)
        {
          for (var x = 0; x < (tessellationX + 1); x++)
          {
            var baseVertex = vertices[vertexCount - numVertices];
            var position = new Vector3(baseVertex.Position.X, baseVertex.Position.Y, baseVertex.Position.Z);
            var texCoord = new Vector2(uv.X * x / tessellationX, uv.Y * y / tessellationY);
            var normal = baseVertex.Normal;
            vertices[vertexCount++] = new VertexPositionNormalTexture(position, -normal, texCoord);
          }
        }

        for (var y = 0; y < tessellationY; y++)
        {
          for (var x = 0; x < tessellationX; x++)
          {
            var vbase = lineWidth * (y + tessellationY + 1) + x;
            indices[indexCount++] = (vbase + 1);
            indices[indexCount++] = (vbase + lineWidth);
            indices[indexCount++] = (vbase + 1 + lineWidth);
            indices[indexCount++] = (vbase + 1);
            indices[indexCount++] = (vbase);
            indices[indexCount++] = (vbase + lineWidth);
          }
        }
      }

      return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, false) { Name = "Terrain" };
    }

    private Vector3 GetNormal(int x, int y)
    {
      // TODO: Pre-calculate all of these, or cache as they are calculated, and re-use.
      var heightL = GetHeight(x - 1, y);
      var heightR = GetHeight(x + 1, y);
      var heightD = GetHeight(x, y - 1);
      var heightU = GetHeight(x, y + 1);
      var normal = new Vector3(heightL - heightR, 2f, heightD - heightU);
      normal.Normalize();
      return normal;
    }

    private float GetHeight(int x, int y)
    {
      if (x < 0 || x >= HeightMapImage.Description.Height || y < 0 || y >= HeightMapImage.Description.Width)
      {
        return 0;
      }
   
      var height = MaxPixelColor +  heightMapPixelBuffer.GetPixel<Color>(x, y).ToRgb();    
      return height / MaxPixelColor * MaxHeight;
    }
  }
}