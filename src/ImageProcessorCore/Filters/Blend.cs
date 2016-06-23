﻿// <copyright file="Blend.cs" company="James Jackson-South">
// Copyright (c) James Jackson-South and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>-------------------------------------------------------------------------------------------------------------------

namespace ImageProcessorCore
{
    using Processors;

    /// <summary>
    /// Extension methods for the <see cref="Image"/> type.
    /// </summary>
    public static partial class ImageExtensions
    {
        /// <summary>
        /// Combines the given image together with the current one by blending their pixels.
        /// </summary>
        /// <param name="source">The image this method extends.</param>
        /// <param name="image">
        /// The image to blend with the currently processing image. 
        /// Disposal of this image is the responsibility of the developer.
        /// </param>
        /// <param name="percent">The opacity of the image image to blend. Must be between 0 and 100.</param>
        /// <param name="progressHandler">A delegate which is called as progress is made processing the image.</param>
        /// <returns>The <see cref="Image"/>.</returns>
        public static Image Blend(this Image source, ImageBase image, int percent = 50, ProgressEventHandler progressHandler = null)
        {
            return Blend(source, image, percent, source.Bounds, progressHandler);
        }

        /// <summary>
        /// Combines the given image together with the current one by blending their pixels.
        /// </summary>
        /// <param name="source">The image this method extends.</param>
        /// <param name="image">
        /// The image to blend with the currently processing image. 
        /// Disposal of this image is the responsibility of the developer.
        /// </param>
        /// <param name="percent">The opacity of the image image to blend. Must be between 0 and 100.</param>
        /// <param name="rectangle">
        /// The <see cref="Rectangle"/> structure that specifies the portion of the image object to alter.
        /// </param>
        /// <param name="progressHandler">A delegate which is called as progress is made processing the image.</param>
        /// <returns>The <see cref="Image"/>.</returns>
        public static Image Blend(this Image source, ImageBase image, int percent, Rectangle rectangle, ProgressEventHandler progressHandler = null)
        {
            BlendProcessor processor = new BlendProcessor(image, percent);
            processor.OnProgress += progressHandler;

            try
            {
                return source.Process(rectangle, processor);
            }
            finally
            {
                processor.OnProgress -= progressHandler;
            }
        }
    }
}
