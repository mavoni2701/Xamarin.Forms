﻿using System.ComponentModel;
using System.Threading.Tasks;
using Android.Widget;
using AScaleType = Android.Widget.ImageView.ScaleType;
using ARect = Android.Graphics.Rect;
using System;
using Xamarin.Forms.Internals;
using AViewCompat = Android.Support.V4.View.ViewCompat;

namespace Xamarin.Forms.Platform.Android.FastRenderers
{
	public static class ImageElementManager
	{
		public static void Init(IVisualElementRenderer renderer)
		{
			renderer.ElementPropertyChanged += OnElementPropertyChanged;
			renderer.ElementChanged += OnElementChanged;

			if(renderer is ILayoutChanges layoutChanges)
				layoutChanges.LayoutChange += OnLayoutChange;
		}

		static void OnLayoutChange(object sender, global::Android.Views.View.LayoutChangeEventArgs e)
		{
			if(sender is IVisualElementRenderer renderer && renderer.View is ImageView imageView)
				AViewCompat.SetClipBounds(imageView, imageView.GetScaleType() == AScaleType.CenterCrop ? new ARect(0, 0, e.Right - e.Left, e.Bottom - e.Top) : null);
		}

		public static void Dispose(IVisualElementRenderer renderer)
		{
			renderer.ElementPropertyChanged -= OnElementPropertyChanged;
			renderer.ElementChanged -= OnElementChanged;
			if (renderer is ILayoutChanges layoutChanges)
				layoutChanges.LayoutChange -= OnLayoutChange;

			if (renderer is IImageRendererController imageRenderer)
				imageRenderer.SetFormsAnimationDrawable(null);

			if (renderer.View is ImageView imageView)
			{
				imageView.SetImageDrawable(null);
				imageView.Reset();
			}
		}

		async static void OnElementChanged(object sender, VisualElementChangedEventArgs e)
		{
			var renderer = (sender as IVisualElementRenderer);
			var view = renderer.View as ImageView;
			var newImageElementManager = e.NewElement as IImageElement;
			var oldImageElementManager = e.OldElement as IImageElement;
			var rendererController = renderer as IImageRendererController;

			await TryUpdateBitmap(rendererController, view, newImageElementManager, oldImageElementManager);
			UpdateAspect(rendererController, view, newImageElementManager, oldImageElementManager);

			if (!rendererController.IsDisposed)
			{
				ElevationHelper.SetElevation(view, renderer.Element);
			}
		}

		async static void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var renderer = (sender as IVisualElementRenderer);
			var ImageElementManager = (IImageElement)renderer.Element;
			var imageController = (IImageController)renderer.Element;

			if (e.IsOneOf(Image.SourceProperty, Button.ImageSourceProperty))
			{
				try
				{
					await TryUpdateBitmap(renderer as IImageRendererController, (ImageView)renderer.View, (IImageElement)renderer.Element).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Log.Warning(renderer.GetType().Name, "Error loading image: {0}", ex);
				}
				finally
				{
					if(imageController != null)
						imageController?.SetIsLoading(false);
				}
			}
			else if (e.Is(Image.AspectProperty))
			{
				UpdateAspect(renderer as IImageRendererController, (ImageView)renderer.View, (IImageElement)renderer.Element);
			}
			else if (e.Is(Image.IsAnimationPlayingProperty))
				await StartStopAnimation(renderer, imageController, ImageElementManager).ConfigureAwait(false);
		}

		async static Task StartStopAnimation(
			IVisualElementRenderer renderer,
			IImageController imageController,
			IImageElement imageElement)
		{
			IImageRendererController imageRendererController = renderer as IImageRendererController;
			var view = renderer.View as ImageView;
			if (imageRendererController.IsDisposed || imageElement == null || view == null || view.IsDisposed())
				return;

			if (imageElement.IsLoading)
				return;

			if (!(view.Drawable is IFormsAnimationDrawable) && imageElement.IsAnimationPlaying)
				await TryUpdateBitmap(imageRendererController, view, imageElement);

			if (view.Drawable is IFormsAnimationDrawable animation)
			{
				if (imageElement.IsAnimationPlaying && !animation.IsRunning)
					animation.Start();
				else if (!imageElement.IsAnimationPlaying && animation.IsRunning)
					animation.Stop();
			}
		}


		async static Task TryUpdateBitmap(IImageRendererController rendererController, ImageView Control, IImageElement newImage, IImageElement previous = null)
		{
			if (newImage == null || rendererController.IsDisposed)
			{
				return;
			}

			if (Control.Drawable is IFormsAnimationDrawable currentAnimation)
			{
				rendererController.SetFormsAnimationDrawable(currentAnimation);
				currentAnimation.Stop();
			}
			else
			{
				rendererController.SetFormsAnimationDrawable(null);
			}

			await Control.UpdateBitmap(newImage, previous).ConfigureAwait(false);

			if (Control.Drawable is IFormsAnimationDrawable updatedAnimation)
			{
				if (newImage.IsAnimationAutoPlay)
					updatedAnimation.Start();
			}
		}

		internal static void OnAnimationStopped(IImageController imageElement, FormsAnimationDrawableStateEventArgs e)
		{			
			if (imageElement != null && e.Finished)
				imageElement.OnAnimationFinishedPlaying();
		}

		static void UpdateAspect(IImageRendererController rendererController, ImageView Control, IImageElement newImage, IImageElement previous = null)
		{
			if (newImage == null || rendererController.IsDisposed)
			{
				return;
			}

			ImageView.ScaleType type = newImage.Aspect.ToScaleType();
			Control.SetScaleType(type);
		}
	}
}