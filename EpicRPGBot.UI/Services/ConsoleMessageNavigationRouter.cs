using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class ConsoleMessageNavigationRouter
    {
        private readonly IReadOnlyDictionary<DiscordTabRole, NavigationRoute> _routes;

        public ConsoleMessageNavigationRouter(IEnumerable<NavigationRoute> routes)
        {
            var routeMap = new Dictionary<DiscordTabRole, NavigationRoute>();
            foreach (var route in routes ?? Array.Empty<NavigationRoute>())
            {
                if (route?.Navigator != null && route.TabRole != DiscordTabRole.Unknown)
                {
                    routeMap[route.TabRole] = route;
                }
            }

            _routes = routeMap;
        }

        public async Task<bool> NavigateAsync(
            DiscordMessageReference reference,
            CancellationToken cancellationToken = default)
        {
            if (reference?.IsComplete != true || !_routes.TryGetValue(reference.TabRole, out var route))
            {
                return false;
            }

            if (route.SelectTabAsync != null)
            {
                await route.SelectTabAsync(cancellationToken);
            }

            return await route.Navigator.NavigateToMessageAsync(reference, cancellationToken);
        }
    }

    public sealed class NavigationRoute
    {
        public NavigationRoute(
            DiscordTabRole tabRole,
            Func<CancellationToken, Task> selectTabAsync,
            IDiscordMessageNavigator navigator)
        {
            TabRole = tabRole;
            SelectTabAsync = selectTabAsync;
            Navigator = navigator;
        }

        public DiscordTabRole TabRole { get; }
        public Func<CancellationToken, Task> SelectTabAsync { get; }
        public IDiscordMessageNavigator Navigator { get; }
    }
}
