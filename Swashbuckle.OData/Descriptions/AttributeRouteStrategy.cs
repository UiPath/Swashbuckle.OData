using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNet.OData.Routing.Template;
using Microsoft.Extensions.DependencyInjection;

namespace Swashbuckle.OData.Descriptions
{
    /// <summary>
    /// Creates ODataActionDescriptors from the set of ODataRoute attributes in the API.
    /// </summary>
    internal class AttributeRouteStrategy : IODataActionDescriptorExplorer
    {
        public IEnumerable<ODataActionDescriptor> Generate(HttpConfiguration httpConfig)
        {
            return httpConfig.GetODataRoutes().SelectMany(oDataRoute => GetODataActionDescriptorsFromAttributeRoutes(oDataRoute, httpConfig));
        }

        private static IEnumerable<ODataActionDescriptor> GetODataActionDescriptorsFromAttributeRoutes(ODataRoute oDataRoute, HttpConfiguration httpConfig)
        {
            Contract.Requires(oDataRoute != null);
            Contract.Requires(oDataRoute.Constraints != null);

            var rootContainer = httpConfig.GetODataRootContainer(oDataRoute);
            var routingConventions = rootContainer.GetServices<IODataRoutingConvention>();
            var attributeRoutingConvention = routingConventions.OfType<AttributeRoutingConvention>().SingleOrDefault();
            var result = new List<ODataActionDescriptor>();

            if (attributeRoutingConvention != null)
            {
                var collection = attributeRoutingConvention.GetInstanceField<ICollection>("_attributeMappings", true);

                foreach (dynamic pair in collection)
                {
                    var value = ReflectionExtensions.StaticGetInstanceField(pair, "value", true);
                    var httpDescriptor = ReflectionExtensions.StaticGetInstanceField(value, "innerDescriptor", true);
                    var odataDescriptor = GetODataActionDescriptorFromAttributeRoute(httpDescriptor, oDataRoute, httpConfig);

                    if (odataDescriptor == null)
                        continue;

                    result.Add(odataDescriptor);
                }
            }

            return result;
        }

        private static ODataActionDescriptor GetODataActionDescriptorFromAttributeRoute(HttpActionDescriptor actionDescriptor, ODataRoute oDataRoute, HttpConfiguration httpConfig)
        {
            Contract.Requires(actionDescriptor != null);
            Contract.Requires(oDataRoute != null);
            Contract.Ensures(Contract.Result<ODataActionDescriptor>() != null);

            var odataRoutePrefixAttribute = actionDescriptor.ControllerDescriptor.GetCustomAttributes<ODataRoutePrefixAttribute>()?.FirstOrDefault();
            var odataRouteAttribute = actionDescriptor.GetCustomAttributes<ODataRouteAttribute>()?.FirstOrDefault();

            Contract.Assume(odataRouteAttribute != null);
            var pathTemplate = HttpUtility.UrlDecode(oDataRoute.GetRoutePrefix().AppendUriSegment(GetODataPathTemplate(odataRoutePrefixAttribute?.Prefix, odataRouteAttribute.PathTemplate)));
            Contract.Assume(pathTemplate != null);

            return new ODataActionDescriptor(actionDescriptor, oDataRoute, pathTemplate, CreateHttpRequestMessage(actionDescriptor, oDataRoute, httpConfig));
        }

        private static string GetODataPathTemplate(string prefix, string pathTemplate)
        {
            if (pathTemplate.StartsWith("/", StringComparison.Ordinal))
            {
                return pathTemplate.Substring(1);
            }

            if (string.IsNullOrEmpty(prefix))
            {
                return pathTemplate;
            }

            if (prefix.StartsWith("/", StringComparison.Ordinal))
            {
                prefix = prefix.Substring(1);
            }

            if (string.IsNullOrEmpty(pathTemplate))
            {
                return prefix;
            }

            if (pathTemplate.StartsWith("(", StringComparison.Ordinal))
            {
                return prefix + pathTemplate;
            }

            return prefix + "/" + pathTemplate;
        }

        private static HttpRequestMessage CreateHttpRequestMessage(HttpActionDescriptor actionDescriptor, ODataRoute oDataRoute, HttpConfiguration httpConfig)
        {
            Contract.Requires(httpConfig != null);
            Contract.Requires(oDataRoute != null);
            Contract.Requires(httpConfig != null);
            Contract.Ensures(Contract.Result<HttpRequestMessage>() != null);

            Contract.Assume(oDataRoute.Constraints != null);

            var httpRequestMessage = new HttpRequestMessage(actionDescriptor.SupportedHttpMethods.First(), "http://any/");

            var requestContext = new HttpRequestContext
            {
                Configuration = httpConfig
            };
            httpRequestMessage.SetConfiguration(httpConfig);
            httpRequestMessage.SetRequestContext(requestContext);

            var httpRequestMessageProperties = httpRequestMessage.ODataProperties();
            Contract.Assume(httpRequestMessageProperties != null);
            httpRequestMessage.CreateRequestContainer(oDataRoute.PathRouteConstraint.RouteName);
            return httpRequestMessage;
        }
    }
}