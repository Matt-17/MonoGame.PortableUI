#if ANDROID
using System;
using System.Collections.Generic;
using System.Globalization;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI
{
    /// <summary>
    ///     <see cref="ITextSelectionMenu"/> as Android's own floating text toolbar (a floating action mode on the
    ///     game view, Android 6+): the system's look, animation and overflow menu, Share, and the installed
    ///     text actions (Translate, …) like in any EditText. Set it as <see cref="ScreenEngineOptions.TextSelectionMenu"/>.
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("android23.0")]
    public sealed class AndroidTextSelectionMenu : ITextSelectionMenu
    {
        // Android's EditText orders its entries like this (Editor.MENU_ITEM_ORDER_*).
        private const int OrderCut = 4, OrderCopy = 5, OrderPaste = 6, OrderShare = 7, OrderSelectAll = 8, OrderProcessText = 100;
        private const int ShareId = 0x5E1EC7;

        private readonly View _view;
        private readonly Callback _callback;
        private ActionMode? _mode;
        private TextSelectionMenuRequest? _request;

        /// <summary>
        ///     Also list the apps that act on selected text (Translate, Read aloud, …) in the overflow menu. Off by
        ///     default, like .NET MAUI's Entry. Android 11+ only shows apps the manifest declares a
        ///     <c>&lt;queries&gt;</c> entry for (action <c>android.intent.action.PROCESS_TEXT</c>, type text/plain).
        /// </summary>
        public bool ShowTextActions { get; set; }

        /// <param name="gameView">MonoGame's game view.</param>
        public AndroidTextSelectionMenu(View gameView)
        {
            _view = gameView ?? throw new ArgumentNullException(nameof(gameView));
            _callback = new Callback(this);
        }

        public void Show(TextSelectionMenuRequest request)
        {
            _view.Post(() =>
            {
                var sameEntries = _request != null && SameEntries(_request, request);
                _request = request;
                if (_mode == null)
                {
                    _mode = _view.StartActionMode(_callback, ActionModeType.Floating);
                    return;
                }
                // Moved: reposition; other entries: rebuild the menu in place (no close-and-reopen flicker).
                if (!sameEntries)
                    _mode.Invalidate();
                _mode.InvalidateContentRect();
                // Back after a handle drag (Suspend).
                _mode.Hide(0);
            });
        }

        public void Hide()
        {
            _view.Post(() =>
            {
                var mode = _mode;
                _mode = null;
                _request = null;
                mode?.Finish();
            });
        }

        // Android's EditText does the same while a handle moves; the longest hide is about 3 s, so the drag
        // keeps calling this.
        public void Suspend() => _view.Post(() => _mode?.Hide(MaxHideMilliseconds));

        private const long MaxHideMilliseconds = 3000;

        private static bool SameEntries(TextSelectionMenuRequest a, TextSelectionMenuRequest b)
        {
            if (a.SelectedText != b.SelectedText || a.Actions.Count != b.Actions.Count)
                return false;
            for (var i = 0; i < a.Actions.Count; i++)
            {
                if (a.Actions[i] != b.Actions[i])
                    return false;
            }
            return true;
        }

        private void Populate(IMenu menu)
        {
            menu.Clear();
            if (_request is not { } request || _view.Context is not { } context)
                return;
            foreach (var action in request.Actions)
            {
                var (id, itemOrder, title) = action switch
                {
                    TextSelectionAction.Cut => (Android.Resource.Id.Cut, OrderCut, Android.Resource.String.Cut),
                    TextSelectionAction.Copy => (Android.Resource.Id.Copy, OrderCopy, Android.Resource.String.Copy),
                    TextSelectionAction.Paste => (Android.Resource.Id.Paste, OrderPaste, Android.Resource.String.Paste),
                    _ => (Android.Resource.Id.SelectAll, OrderSelectAll, Android.Resource.String.SelectAll)
                };
                menu.Add(0, id, itemOrder, context.GetString(title))!
                    .SetShowAsAction(action == TextSelectionAction.SelectAll ? ShowAsAction.IfRoom : ShowAsAction.Always);
            }
            if (string.IsNullOrEmpty(request.SelectedText))
                return;
            menu.Add(0, ShareId, OrderShare, ShareLabel())!.SetShowAsAction(ShowAsAction.IfRoom);
            if (!ShowTextActions)
                return;
            // The apps that act on selected text (Translate, …), read-only like in a non-editable TextView.
            var pm = context.PackageManager;
            if (pm == null)
                return;
            var query = new Intent(Intent.ActionProcessText).SetType("text/plain");
            var order = OrderProcessText;
            foreach (var info in pm.QueryIntentActivities(query, (PackageInfoFlags)0) ?? (IList<ResolveInfo>)Array.Empty<ResolveInfo>())
            {
                if (info.ActivityInfo is not { } activity)
                    continue;
                var intent = new Intent(Intent.ActionProcessText)
                    .SetType("text/plain")
                    .SetClassName(activity.PackageName!, activity.Name!)
                    .PutExtra(Intent.ExtraProcessText, request.SelectedText)
                    .PutExtra(Intent.ExtraProcessTextReadonly, true);
                menu.Add(0, IMenu.None, order++, info.LoadLabel(pm))!.SetIntent(intent).SetShowAsAction(ShowAsAction.Never);
            }
        }

        // Android has no public string for "Share"; EditText uses its internal one.
        private static string ShareLabel() => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? "Teilen" : "Share";

        private bool OnItem(ActionMode mode, IMenuItem item)
        {
            var request = _request;
            if (request == null || _view.Context is not { } context)
                return false;
            if (item.Intent is { } processText)
            {
                context.StartActivity(processText);
                return true;
            }
            if (item.ItemId == ShareId)
            {
                var send = new Intent(Intent.ActionSend).SetType("text/plain").PutExtra(Intent.ExtraText, request.SelectedText);
                context.StartActivity(Intent.CreateChooser(send, (string?)null));
                return true;
            }
            TextSelectionAction? action = item.ItemId switch
            {
                Android.Resource.Id.Cut => TextSelectionAction.Cut,
                Android.Resource.Id.Copy => TextSelectionAction.Copy,
                Android.Resource.Id.Paste => TextSelectionAction.Paste,
                Android.Resource.Id.SelectAll => TextSelectionAction.SelectAll,
                _ => null
            };
            if (action is not { } chosen)
                return false;
            request.Invoke(chosen);
            return true;
        }

        private void OnDestroyed(ActionMode mode)
        {
            if (!ReferenceEquals(mode, _mode))
                return;
            var request = _request;
            _mode = null;
            _request = null;
            request?.Dismissed();
        }

        private sealed class Callback : ActionMode.Callback2
        {
            private readonly AndroidTextSelectionMenu _owner;

            public Callback(AndroidTextSelectionMenu owner) => _owner = owner;

            public override bool OnCreateActionMode(ActionMode? mode, IMenu? menu)
            {
                if (menu != null)
                    _owner.Populate(menu);
                return true;
            }

            public override bool OnPrepareActionMode(ActionMode? mode, IMenu? menu)
            {
                if (menu != null)
                    _owner.Populate(menu);
                return true;
            }

            public override bool OnActionItemClicked(ActionMode? mode, IMenuItem? item)
                => mode != null && item != null && _owner.OnItem(mode, item);

            public override void OnDestroyActionMode(ActionMode? mode)
            {
                if (mode != null)
                    _owner.OnDestroyed(mode);
            }

            public override void OnGetContentRect(ActionMode? mode, View? view, Android.Graphics.Rect? outRect)
            {
                if (outRect == null || _owner._request is not { } request)
                    return;
                var r = request.ContentRect;
                outRect.Set((int)MathF.Floor(r.Left), (int)MathF.Floor(r.Top), (int)MathF.Ceiling(r.Right), (int)MathF.Ceiling(r.Bottom));
            }
        }
    }
}
#endif
