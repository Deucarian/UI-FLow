using System;
using System.Reflection;
using Deucarian.UIFlow;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.UIFlow.Tests.EditMode
{
    public sealed class UIFlowValueAndCatalogTests
    {
        [Test]
        public void RouteIdRejectsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new UIFlowRouteId(" "));
        }

        [Test]
        public void RouteIdUsesValueEquality()
        {
            var left = new UIFlowRouteId("settings");
            var right = new UIFlowRouteId("settings");

            Assert.AreEqual(left, right);
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
            Assert.AreEqual("settings", left.ToString());
        }

        [Test]
        public void ChannelIdRejectsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new UIFlowChannelId(""));
        }

        [Test]
        public void CatalogLookupFindsStableRouteId()
        {
            UIFlowRoute route = TestRoute("route-a");
            UIFlowRouteCatalog catalog = ScriptableObject.CreateInstance<UIFlowRouteCatalog>();
            SetField(catalog, "_routes", new[] { route });

            UIFlowRoute found;
            Assert.IsTrue(catalog.TryGetRoute(new UIFlowRouteId("route-a"), out found));
            Assert.AreSame(route, found);
        }

        [Test]
        public void CatalogValidationReportsDuplicateRouteIds()
        {
            UIFlowRoute first = TestRoute("duplicate");
            UIFlowRoute second = TestRoute("duplicate");
            UIFlowRouteCatalog catalog = ScriptableObject.CreateInstance<UIFlowRouteCatalog>();
            SetField(catalog, "_routes", new[] { first, second });

            Assert.IsTrue(catalog.ValidateCatalog()[0].Contains("Duplicate route ID"));
        }

        private static UIFlowRoute TestRoute(string id)
        {
            UIFlowRoute route = ScriptableObject.CreateInstance<UIFlowRoute>();
            route.name = id;
            SetField(route, "_routeId", new UIFlowRouteId(id));
            SetField(route, "_targetChannel", UIFlowChannelId.Main);
            return route;
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, "Missing field " + fieldName);
            field.SetValue(instance, value);
        }
    }
}
