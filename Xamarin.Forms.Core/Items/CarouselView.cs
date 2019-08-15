﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Xamarin.Forms.Platform;

namespace Xamarin.Forms
{
	public class ScrolledDirectionEventArgs : EventArgs
	{
		public ScrolledDirectionEventArgs(ScrollDirection direction, double newValue)
		{
			Direction = direction;
			NewValue = newValue;

		}
		public double NewValue { get; private set; }
		public ScrollDirection Direction { get; private set; }
	}

	public enum ScrollDirection
	{
		Left,
		Right,
		Up,
		Down
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface ICarouselViewController : IViewController
	{
		void SendScrolled(double value, ScrollDirection direction);
		void SetCurrentItem(object item);
	}

	[RenderWith(typeof(_CarouselViewRenderer))]
	public class CarouselView : ItemsView, ICarouselViewController
	{
		public static readonly BindableProperty IsSwipeEnabledProperty = BindableProperty.Create(nameof(IsSwipeEnabled), typeof(bool), typeof(CarouselView), true);

		public bool IsSwipeEnabled
		{
			get { return (bool)GetValue(IsSwipeEnabledProperty); }
			set { SetValue(IsSwipeEnabledProperty, value); }
		}

		public static readonly BindableProperty ItemSpacingProperty =
				BindableProperty.Create(nameof(ItemSpacing), typeof(double), typeof(CarouselView), 0.0);

		public double ItemSpacing
		{
			get { return (double)GetValue(ItemSpacingProperty); }
			set { SetValue(ItemSpacingProperty, value); }
		}

		public static readonly BindableProperty AnimateTransitionProperty =
		BindableProperty.Create(nameof(AnimateTransition), typeof(bool), typeof(CarouselView), true);

		public bool AnimateTransition
		{
			get { return (bool)GetValue(AnimateTransitionProperty); }
			set { SetValue(AnimateTransitionProperty, value); }
		}

		public static readonly BindableProperty CurrentItemProperty =
		BindableProperty.Create(nameof(CurrentItem), typeof(object), typeof(CarouselView), default(object), 
			propertyChanged: CurrentItemPropertyChanged);

		public static readonly BindableProperty CurrentItemChangedCommandProperty =
			BindableProperty.Create(nameof(CurrentItemChangedCommand), typeof(ICommand), typeof(CarouselView));

		public static readonly BindableProperty CurrentItemChangedCommandParameterProperty =
			BindableProperty.Create(nameof(CurrentItemChangedCommandParameter), typeof(object), typeof(CarouselView));

		public object CurrentItem
		{
			get => GetValue(CurrentItemProperty);
			set => SetValue(CurrentItemProperty, value);
		}

		public ICommand CurrentItemChangedCommand
		{
			get => (ICommand)GetValue(CurrentItemChangedCommandProperty);
			set => SetValue(CurrentItemChangedCommandProperty, value);
		}

		public object CurrentItemChangedCommandParameter
		{
			get => GetValue(CurrentItemChangedCommandParameterProperty);
			set => SetValue(CurrentItemChangedCommandParameterProperty, value);
		}

		public event EventHandler<CurrentItemChangedEventArgs> CurrentItemChanged;

		static void CurrentItemPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var carouselView = (CarouselView)bindable;

			var args = new CurrentItemChangedEventArgs(oldValue,newValue);

			var command = carouselView.CurrentItemChangedCommand;

			if (command != null)
			{
				var commandParameter = carouselView.CurrentItemChangedCommandParameter;

				if (command.CanExecute(commandParameter))
				{
					command.Execute(commandParameter);
				}
			}

			carouselView.SetValueCore(PositionProperty, GetPositionForItem(carouselView, newValue));

			carouselView.CurrentItemChanged?.Invoke(carouselView, args);

			carouselView.OnCurrentItemChanged(args);
		}

		protected virtual void OnCurrentItemChanged(EventArgs args)
		{
		}

		public static readonly BindableProperty PositionProperty =
		BindableProperty.Create(nameof(Position), typeof(int), typeof(CarouselView), default(int), BindingMode.TwoWay,
			propertyChanged: PositionPropertyChanged);

		public static readonly BindableProperty PositionChangedCommandProperty =
			BindableProperty.Create(nameof(PositionChangedCommand), typeof(ICommand), typeof(CarouselView));

		public static readonly BindableProperty PositionChangedCommandParameterProperty =
			BindableProperty.Create(nameof(PositionChangedCommandParameter), typeof(object),
				typeof(CarouselView));

		public int Position
		{
			get => (int)GetValue(PositionProperty);
			set => SetValue(PositionProperty, value);
		}

		public ICommand PositionChangedCommand
		{
			get => (ICommand)GetValue(PositionChangedCommandProperty);
			set => SetValue(PositionChangedCommandProperty, value);
		}

		public object PositionChangedCommandParameter
		{
			get => GetValue(PositionChangedCommandParameterProperty);
			set => SetValue(PositionChangedCommandParameterProperty, value);
		}

		public event EventHandler<PositionChangedEventArgs> PositionChanged;

		protected virtual void OnPositionChanged(PositionChangedEventArgs args)
		{
			ScrollTo(args.CurrentPosition, animate: AnimateTransition);
		}

		static void PositionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var carousel = (CarouselView)bindable;

			var args = new PositionChangedEventArgs((int)oldValue, (int)newValue);

			var command = carousel.PositionChangedCommand;

			if (command != null)
			{
				var commandParameter = carousel.PositionChangedCommandParameter;

				if (command.CanExecute(commandParameter))
				{
					command.Execute(commandParameter);
				}
			}

			carousel.PositionChanged?.Invoke(carousel, args);

			carousel.OnPositionChanged(args);
		}

		public event EventHandler<ScrolledDirectionEventArgs> Scrolled;

		public CarouselView()
		{
			CollectionView.VerifyCollectionViewFlagEnabled(constructorHint: nameof(CarouselView));
			ItemsLayout = new ListItemsLayout(ItemsLayoutOrientation.Horizontal)
			{
				SnapPointsType = SnapPointsType.MandatorySingle,
				SnapPointsAlignment = SnapPointsAlignment.Center
			};
		}

		protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			if (propertyName.Equals(CarouselView.CurrentItemProperty.PropertyName))
			{

			}
			base.OnPropertyChanged(propertyName);
		}

		static int GetPositionForItem(CarouselView carouselView, object item)
		{
			var itemSource = carouselView.ItemsSource as IList;

			for (int n = 0; n < itemSource.Count; n++)
			{
				if (itemSource[n] == item)
				{
					return n;
				}
			}
			return 0;
		}

		public void SendScrolled(double value, ScrollDirection direction)
		{
			Scrolled?.Invoke(this, new ScrolledDirectionEventArgs(direction, value));
		}

		public void SetCurrentItem(object item)
		{
			CurrentItem = item;
		}
	}
}