.PHONY: generate
generate:
	dotnet tool restore
	dotnet nswag openapi2csclient \
		/input:Boilerplate.Api.Web/openapi.yaml \
		/output:Boilerplate.Api.Web/Models/Models.Generated.cs \
		/namespace:Boilerplate.Api.Web.Models \
		/GenerateClientClasses:false \
		/GenerateClientInterfaces:false \
		/GenerateExceptionClasses:false \
		/JsonLibrary:SystemTextJson \
		/GenerateNullableReferenceTypes:true \
		/GenerateOptionalPropertiesAsNullable:true \
		/GenerateDataAnnotations:true \
		/GenerateJsonMethods:false

.PHONY: clean
clean:
	dotnet clean Boilerplate.slnx

.PHONY: build
build:
	dotnet build Boilerplate.slnx

.PHONY: test
test:
	dotnet test Boilerplate.slnx
