using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Reflection;



namespace FCG.Presentation.Swagger
{
    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (!context.Type.IsEnum)
                return;

            schema.Enum.Clear();

            var enumValues = Enum.GetValues(context.Type);

            foreach (var value in enumValues)
            {
                var name = Enum.GetName(context.Type, value);
                var field = context.Type.GetField(name!);

                var description = field?
                    .GetCustomAttribute<DescriptionAttribute>()?
                    .Description ?? name;

                schema.Enum.Add(new OpenApiString(description));
            }
        }
    }
    
}