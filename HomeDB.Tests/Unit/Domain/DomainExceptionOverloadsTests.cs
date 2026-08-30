using HomeDB.Domain.Exceptions;

namespace HomeDB.Tests.Unit.Domain
{
    //Tests unitarios de las excepciones de dominio que tienen más de un constructor, para
    //asegurar que cada overload arma el mensaje correcto (ExceptionHandlerMiddleware expone
    //ese mensaje tal cual en el cuerpo de la respuesta de error).
    public sealed class DomainExceptionOverloadsTests
    {
        [Fact]
        public void UserNotFoundException_WithUsername_MentionsTheUsername()
        {
            UserNotFoundException exception = new UserNotFoundException("john.doe");

            Assert.Equal("User 'john.doe' was not found.", exception.Message);
        }

        [Fact]
        public void UserNotFoundException_WithId_MentionsTheId()
        {
            UserNotFoundException exception = new UserNotFoundException(42);

            Assert.Equal("User with id 42 was not found.", exception.Message);
        }

        [Fact]
        public void UnauthorizedException_WithUserIdOnly_UsesGenericResourceWording()
        {
            UnauthorizedException exception = new UnauthorizedException(7);

            Assert.Equal("User 7 is not authorized to access this resource.", exception.Message);
        }

        [Fact]
        public void UnauthorizedException_WithUserIdAndResourceId_MentionsBothIds()
        {
            UnauthorizedException exception = new UnauthorizedException(7, 99);

            Assert.Equal("User 7 is not authorized to access resource 99.", exception.Message);
        }

        [Fact]
        public void IncompleteUploadException_WithCustomMessage_PrefixesIt()
        {
            IncompleteUploadException exception = new IncompleteUploadException("missing chunk 2");

            Assert.Equal("Incomplete upload: missing chunk 2", exception.Message);
        }

        [Fact]
        public void IncompleteUploadException_WithSessionDetails_FormatsCounts()
        {
            Guid sessionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            IncompleteUploadException exception = new IncompleteUploadException(sessionId, receivedCount: 2, totalChunks: 5);

            Assert.Equal(
                $"Upload session {sessionId} is incomplete. Received 2 of 5 chunks.",
                exception.Message);
        }
    }
}
