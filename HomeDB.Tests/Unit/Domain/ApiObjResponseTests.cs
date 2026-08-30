using HomeDB.Domain.Common;

namespace HomeDB.Tests.Unit.Domain
{
    //Tests unitarios de los factory methods de ApiObjResponse<T>, usados en toda la API para
    //construir las respuestas de éxito/error de forma consistente.
    public sealed class ApiObjResponseTests
    {
        [Fact]
        public void Success_WithData_SetsResultTrueAndDataAndClearsErrorFields()
        {
            ApiObjResponse<string> response = ApiObjResponse<string>.Success("some-data");

            Assert.True(response.Result);
            Assert.Equal("some-data", response.Data);
            Assert.Null(response.ErrorCode);
            Assert.Null(response.ErrorMessage);
        }

        [Fact]
        public void Success_WithNullData_AllowsNullDataAndStillReportsSuccess()
        {
            ApiObjResponse<string?> response = ApiObjResponse<string?>.Success(null);

            Assert.True(response.Result);
            Assert.Null(response.Data);
        }

        [Fact]
        public void Success_WithValueType_StoresTheValue()
        {
            ApiObjResponse<int> response = ApiObjResponse<int>.Success(42);

            Assert.True(response.Result);
            Assert.Equal(42, response.Data);
        }

        [Fact]
        public void Failure_SetsResultFalseAndErrorCodeAndMessageAndDefaultData()
        {
            ApiObjResponse<string> response = ApiObjResponse<string>.Failure(
                ApiErrorCodes.FileNotFound, "File not found");

            Assert.False(response.Result);
            Assert.Null(response.Data);
            Assert.Equal(ApiErrorCodes.FileNotFound, response.ErrorCode);
            Assert.Equal("File not found", response.ErrorMessage);
        }

        [Fact]
        public void Failure_WithValueTypeData_DataIsDefaultValue()
        {
            ApiObjResponse<int> response = ApiObjResponse<int>.Failure(
                ApiErrorCodes.InternalError, "Something went wrong");

            Assert.False(response.Result);
            Assert.Equal(default, response.Data);
        }
    }
}
