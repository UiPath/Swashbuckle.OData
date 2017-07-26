using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.OData;
using Swashbuckle.Swagger;

namespace Swashbuckle.OData.Descriptions
{
    internal class MapToODataActionParameter : IParameterMapper
    {
        public HttpParameterDescriptor Map(Parameter swaggerParameter, int parameterIndex, HttpActionDescriptor actionDescriptor)
        {
            var required = swaggerParameter.required;
            Contract.Assume(required != null);

            if (IsODataActionParameter(swaggerParameter) && HasODataActionParameterDescription(actionDescriptor))
            {
                var odataActionParametersDescriptor = GetODataActionParameterDescription(actionDescriptor);
                Contract.Assume(odataActionParametersDescriptor != null);
                return new ODataActionParameterDescriptor(odataActionParametersDescriptor.ParameterName, typeof(ODataActionParameters), !required.Value, swaggerParameter.schema, odataActionParametersDescriptor)
                {
                    Configuration = actionDescriptor.ControllerDescriptor.Configuration,
                    ActionDescriptor = actionDescriptor
                };
            }
            return null;
        }
        public static HttpParameterDescriptor GetODataActionParameterDescription(HttpActionDescriptor actionDescriptor)
        {
             return actionDescriptor.GetParameters().SingleOrDefault(descriptor => descriptor.ParameterType == typeof(ODataActionParameters));
        }
 
        public static bool HasODataActionParameterDescription(HttpActionDescriptor actionDescriptor)
        {
            return GetODataActionParameterDescription(actionDescriptor) != null;
        }

        public static bool IsODataActionParameter(Parameter parameter)
        {
            return parameter.@in == "body" && parameter.schema != null && parameter.schema.type == "object";
        }
    }
}