using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Maxwell.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Maxwell.Pages.Common
{
    public class ViewModelBase : LayoutComponentBase
    {
        #region Private Variables

        private bool _isUpdated = true;

        [Inject]
        private IJSRuntime _jsRuntime { get; set; }

        #endregion

        #region Protected Methods

        protected override bool ShouldRender()
        {
            if (_isUpdated)
            {
                _isUpdated = false;
                return true;
            }

            return false;
        }

        protected void UpdateProperty<T>(ref T property, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(property, newValue))
            {
                return;
            }

            property = newValue;
            _isUpdated = true;
        }

        protected void UpdateProperty<T>(ref T property, T newValue, Action<T> action)
        {
            if (EqualityComparer<T>.Default.Equals(property, newValue))
            {
                return;
            }

            property = newValue;
            _isUpdated = true;

            action(newValue);
        }

        protected Func<Task> CreateEventCallbackAsyncCommand(Func<Task> action, string message)
        {
            return async () =>
            {
                await AttemptActionAsync(async () =>
                {
                    await action()
                        .AnyContext();
                }, message)
                .AnyContext();

                await RefreshAsync()
                    .AnyContext();
            };
        }

        protected Func<T, Task> CreateEventCallbackAsyncCommand<T>(Func<T, Task> action, string message)
        {
            return async (T args) =>
            {
                await AttemptActionAsync(async () =>
                {
                    await action(args)
                        .AnyContext();
                }, message)
                .AnyContext();

                await RefreshAsync()
                    .AnyContext();
            };
        }

        protected Func<T1, T2, Task> CreateEventCallbackAsyncCommand<T1, T2>(Func<T1, T2, Task> action, string message)
        {
            return async (T1 t1, T2 t2) =>
            {
                await AttemptActionAsync(async () =>
                {
                    await action(t1, t2)
                        .AnyContext();
                }, message)
                .AnyContext();

                await RefreshAsync()
                    .AnyContext();
            };
        }

        protected Action<T> CreateEventCallbackCommand<T>(Action<T> action, string message)
        {
            return (T args) =>
            {
                AttemptAction(() =>
                {
                    action(args);
                }, message);

                RefreshAsync()
                    .AnyContext();
            };
        }

        protected Action CreateEventCallbackCommand(Action action, string message)
        {
            return () =>
            {
                AttemptAction(() =>
                {
                    action();
                }, message);

                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            };
        }

        protected void AttemptAction(Action action, string message)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _ = _jsRuntime.InvokeAsync<object>("alert", $"{message}: {ex.Message}");
            }

            RefreshAsync()
                .AnyContext();
        }

        protected async Task AttemptActionAsync(Func<Task> actionAsync, string message)
        {
            try
            {
                await actionAsync()
                    .AnyContext();
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeAsync<object>("alert", $"{message}: {ex.Message}")
                    .ConfigureAwait(false);
            }

            await RefreshAsync()
                .AnyContext();
        }

        protected Task RefreshAsync()
        {
            _isUpdated = true;

            return InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }

        #endregion
    }
}
