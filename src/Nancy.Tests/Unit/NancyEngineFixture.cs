namespace Nancy.Tests.Unit
{
    using System;
    using FakeItEasy;    
    using Nancy.Routing;
    using Nancy.Tests.Fakes;
    using Xunit;

    public class NancyEngineFixture
    {
        private readonly INancyEngine engine;
        private readonly IRouteResolver resolver;
        private readonly FakeRoute route;
        private readonly NancyContext context;
        private readonly INancyContextFactory contextFactory;
        private readonly Response response;

        public NancyEngineFixture()
        {
            this.resolver = A.Fake<IRouteResolver>();
            this.response = new Response();
            this.route = new FakeRoute(response);
            this.context = new NancyContext();

            contextFactory = A.Fake<INancyContextFactory>();
            A.CallTo(() => contextFactory.Create()).Returns(context);

            A.CallTo(() => resolver.Resolve(A<NancyContext>.Ignored, A<IRouteCache>.Ignored.Argument)).Returns(new Tuple<Route, DynamicDictionary>(route, DynamicDictionary.Empty));

            this.engine = new NancyEngine(resolver, A.Fake<IRouteCache>(), contextFactory);
        }

        [Fact]
        public void Should_throw_argumentnullexception_when_created_with_null_resolver()
        {
            // Given, When
            var exception =
                Record.Exception(() => new NancyEngine(null, A.Fake<IRouteCache>(), A.Fake<INancyContextFactory>()));

            // Then
            exception.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Should_throw_argumentnullexception_when_created_with_null_routecache()
        {
            // Given, When
            var exception =
                Record.Exception(() => new NancyEngine(A.Fake<IRouteResolver>(), null, A.Fake<INancyContextFactory>()));

            // Then
            exception.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Should_throw_argumentnullexception_when_created_with_null_context_factory()
        {
            // Given, When
            var exception =
                Record.Exception(() => new NancyEngine(A.Fake<IRouteResolver>(), A.Fake<IRouteCache>(), null));

            // Then
            exception.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Should_invoke_resolved_route()
        {
            // Given
            var request = new Request("GET", "/", "http");

            // When
            this.engine.HandleRequest(request);

            // Then
            this.route.ActionWasInvoked.ShouldBeTrue();
        }

        [Fact]
        public void HandleRequest_Should_Throw_ArgumentNullException_When_Given_A_Null_Request()
        {
            var exception = Record.Exception(() => engine.HandleRequest(null));

            // Then
            exception.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void HandleRequest_should_get_context_from_context_factory()
        {
            var request = new Request("GET", "/", "http");

            this.engine.HandleRequest(request);

            A.CallTo(() => this.contextFactory.Create()).MustHaveHappened(Repeated.Once);
        }

        [Fact]
        public void HandleRequest_should_set_correct_response_on_returned_context()
        {
            var request = new Request("GET", "/", "http");

            var result = this.engine.HandleRequest(request);

            result.Response.ShouldBeSameAs(this.response);
        }

        [Fact]
        public void HandleRequest_Null_PreRequest_Should_Not_Throw()
        {
            engine.PreRequestHook = null;
            
            var request = new Request("GET", "/", "http");

            this.engine.HandleRequest(request);
        }

        [Fact]
        public void HandleRequest_Null_PostRequest_Should_Not_Throw()
        {
            engine.PostRequestHook = null;

            var request = new Request("GET", "/", "http");

            this.engine.HandleRequest(request);
        }

        [Fact]
        public void HandleRequest_NonNull_PreRequest_Should_Call_PreRequest_With_Request_In_Context()
        {
            Request passedReqest = null;
            engine.PreRequestHook = (ctx) =>
            {
                passedReqest = ctx.Request;
                return null;
            };
            var request = new Request("GET", "/", "http");

            this.engine.HandleRequest(request);

            passedReqest.ShouldBeSameAs(request);
        }

        [Fact]
        public void HandleRequest_PreRequest_Returns_NonNull_Response_Should_Return_That_Response()
        {
            var response = A.Fake<Response>();
            engine.PreRequestHook = req => response;
            var request = new Request("GET", "/", "http");

            var result = this.engine.HandleRequest(request);

            result.Response.ShouldBeSameAs(response);
        }

        [Fact]
        public void HandleRequest_should_allow_post_request_hook_to_modify_context_items()
        {
            engine.PostRequestHook = ctx => ctx.Items.Add("PostReqTest", new object());
            var request = new Request("GET", "/", "http");

            var result = this.engine.HandleRequest(request);

            result.Items.ContainsKey("PostReqTest").ShouldBeTrue();
        }

        [Fact]
        public void HandleRequest_should_allow_post_request_hook_to_replace_response()
        {
            var newResponse = new Response();
            engine.PreRequestHook = ctx => ctx.Response = newResponse;
            var request = new Request("GET", "/", "http");

            var result = this.engine.HandleRequest(request);

            result.Response.ShouldBeSameAs(newResponse);
        }
    }
}
