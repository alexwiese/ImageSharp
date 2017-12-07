﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Helpers;
using SixLabors.ImageSharp.MetaData.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace SixLabors.ImageSharp.Processing.Processors
{
    /// <summary>
    /// Provides methods that allow the rotating of images.
    /// </summary>
    /// <typeparam name="TPixel">The pixel format.</typeparam>
    internal class RotateProcessor<TPixel> : AffineProcessor<TPixel>
        where TPixel : struct, IPixel<TPixel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RotateProcessor{TPixel}"/> class.
        /// </summary>
        /// <param name="angle">The angle of rotation in degrees.</param>
        public RotateProcessor(float angle)
            : this(angle, KnownResamplers.NearestNeighbor)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RotateProcessor{TPixel}"/> class.
        /// </summary>
        /// <param name="degrees">The angle of rotation in degrees.</param>
        /// <param name="sampler">The sampler to perform the rotating operation.</param>
        public RotateProcessor(float degrees, IResampler sampler)
            : base(sampler)
        {
            this.Degrees = degrees;
        }

        /// <summary>
        /// Gets the angle of rotation in degrees.
        /// </summary>
        public float Degrees { get; }

        /// <inheritdoc/>
        protected override Matrix3x2 GetTransformMatrix()
        {
            // Tansforms are inverted else the output is the opposite of the expected.
            Matrix3x2 matrix = Matrix3x2Extensions.CreateRotationDegrees(this.Degrees, PointF.Empty);
            Matrix3x2.Invert(matrix, out matrix);
            return matrix;
        }

        /// <inheritdoc/>
        protected override void OnApply(ImageFrame<TPixel> source, ImageFrame<TPixel> destination, Rectangle sourceRectangle, Configuration configuration)
        {
            if (this.OptimizedApply(source, destination, configuration))
            {
                return;
            }

            base.OnApply(source, destination, sourceRectangle, configuration);
        }

        /// <inheritdoc/>
        protected override void AfterImageApply(Image<TPixel> source, Image<TPixel> destination, Rectangle sourceRectangle)
        {
            ExifProfile profile = destination.MetaData.ExifProfile;
            if (profile == null)
            {
                return;
            }

            if (MathF.Abs(this.Degrees) < Constants.Epsilon)
            {
                // No need to do anything so return.
                return;
            }

            profile.RemoveValue(ExifTag.Orientation);

            if (profile.GetValue(ExifTag.PixelXDimension) != null)
            {
                profile.SetValue(ExifTag.PixelXDimension, source.Width);
                profile.SetValue(ExifTag.PixelYDimension, source.Height);
            }
        }

        /// <summary>
        /// Rotates the images with an optimized method when the angle is 90, 180 or 270 degrees.
        /// </summary>
        /// <param name="source">The source image.</param>
        /// <param name="destination">The destination image.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>
        /// The <see cref="bool" />
        /// </returns>
        private bool OptimizedApply(ImageFrame<TPixel> source, ImageFrame<TPixel> destination, Configuration configuration)
        {
            if (MathF.Abs(this.Degrees) < Constants.Epsilon)
            {
                // The destination will be blank here so copy all the pixel data over
                source.GetPixelSpan().CopyTo(destination.GetPixelSpan());
                return true;
            }

            if (MathF.Abs(this.Degrees - 90) < Constants.Epsilon)
            {
                this.Rotate90(source, destination, configuration);
                return true;
            }

            if (MathF.Abs(this.Degrees - 180) < Constants.Epsilon)
            {
                this.Rotate180(source, destination, configuration);
                return true;
            }

            if (MathF.Abs(this.Degrees - 270) < Constants.Epsilon)
            {
                this.Rotate270(source, destination, configuration);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Rotates the image 270 degrees clockwise at the centre point.
        /// </summary>
        /// <param name="source">The source image.</param>
        /// <param name="destination">The destination image.</param>
        /// <param name="configuration">The configuration.</param>
        private void Rotate270(ImageFrame<TPixel> source, ImageFrame<TPixel> destination, Configuration configuration)
        {
            int width = source.Width;
            int height = source.Height;
            Rectangle destinationBounds = destination.Bounds();

            Parallel.For(
                0,
                height,
                configuration.ParallelOptions,
                y =>
                {
                    Span<TPixel> sourceRow = source.GetPixelRowSpan(y);
                    for (int x = 0; x < width; x++)
                    {
                        int newX = height - y - 1;
                        newX = height - newX - 1;
                        int newY = width - x - 1;

                        if (destinationBounds.Contains(newX, newY))
                        {
                            destination[newX, newY] = sourceRow[x];
                        }
                    }
                });
        }

        /// <summary>
        /// Rotates the image 180 degrees clockwise at the centre point.
        /// </summary>
        /// <param name="source">The source image.</param>
        /// <param name="destination">The destination image.</param>
        /// <param name="configuration">The configuration.</param>
        private void Rotate180(ImageFrame<TPixel> source, ImageFrame<TPixel> destination, Configuration configuration)
        {
            int width = source.Width;
            int height = source.Height;

            Parallel.For(
                0,
                height,
                configuration.ParallelOptions,
                y =>
                {
                    Span<TPixel> sourceRow = source.GetPixelRowSpan(y);
                    Span<TPixel> targetRow = destination.GetPixelRowSpan(height - y - 1);

                    for (int x = 0; x < width; x++)
                    {
                        targetRow[width - x - 1] = sourceRow[x];
                    }
                });
        }

        /// <summary>
        /// Rotates the image 90 degrees clockwise at the centre point.
        /// </summary>
        /// <param name="source">The source image.</param>
        /// <param name="destination">The destination image.</param>
        /// <param name="configuration">The configuration.</param>
        private void Rotate90(ImageFrame<TPixel> source, ImageFrame<TPixel> destination, Configuration configuration)
        {
            int width = source.Width;
            int height = source.Height;
            Rectangle destinationBounds = destination.Bounds();

            Parallel.For(
                0,
                height,
                configuration.ParallelOptions,
                y =>
                {
                    Span<TPixel> sourceRow = source.GetPixelRowSpan(y);
                    int newX = height - y - 1;
                    for (int x = 0; x < width; x++)
                    {
                        if (destinationBounds.Contains(newX, x))
                        {
                            destination[newX, x] = sourceRow[x];
                        }
                    }
                });
        }
    }
}