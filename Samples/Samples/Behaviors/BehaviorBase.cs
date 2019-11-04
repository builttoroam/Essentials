﻿using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

/*
 * This code is a slightly modified version of the Xamarin-Forms-sample/Behaviors code provided here:
 * https://github.com/xamarin/xamarin-forms-samples/tree/master/Behaviors/EventToCommandBehavior/EventToCommandBehavior/Behaviors
 */

namespace Samples.Behaviors
{
    public class BehaviorBase<T> : Behavior<T>
        where T : BindableObject
    {
        public T AssociatedObject { get; private set; }

        protected override void OnAttachedTo(T bindable)
        {
            base.OnAttachedTo(bindable);
            AssociatedObject = bindable;

            if (bindable.BindingContext != null)
            {
                BindingContext = bindable.BindingContext;
            }

            bindable.BindingContextChanged += OnBindingContextChanged;
        }

        protected override void OnDetachingFrom(T bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.BindingContextChanged -= OnBindingContextChanged;
            AssociatedObject = null;
        }

        void OnBindingContextChanged(object sender, EventArgs e)
        {
            OnBindingContextChanged();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            BindingContext = AssociatedObject.BindingContext;
        }
    }
}
