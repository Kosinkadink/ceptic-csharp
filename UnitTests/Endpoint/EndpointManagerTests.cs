using Ceptic.Common;
using Ceptic.Endpoint;
using Ceptic.Endpoint.Exceptions;
using Ceptic.Server;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnitTests.Endpoint
{
    public class EndpointManagerTests
    {

        private EndpointEntry basicEndpointEntry = new EndpointEntry((request, values) => new CepticResponse(CepticStatusCode.OK));
        private EndpointManager manager;

        [SetUp]
        public void Setup()
        {
            manager = new EndpointManager(new ServerSettings());
        }

        [Test]
        public void CreateManager_Success()
        {
            // Arrange, Act, Assert
            Assert.That(() => new EndpointManager(new ServerSettings()), Throws.Nothing);
        }

        [Test]
        public void AddCommand_Success()
        {
            // Arrange
            var command = "get";
            // Act
            manager.AddCommand(command);
            // Assert
            Assert.That(manager.GetCommand(command), Is.Not.Null);
        }

        [Test]
        public void GetCommand_Success()
        {
            // Arrange
            var command = "get";
            manager.AddCommand("get");
            // Act
            var entry = manager.GetCommand(command);
            // Assert
            Assert.That(entry, Is.Not.Null);
            Assert.That(command, Is.EqualTo(entry.GetCommand()));
        }

        [Test]
        public void GetCommand_DoesNotExist_IsNull()
        {
            // Arrange
            manager.AddCommand("get");
            // Act
            var entry = manager.GetCommand("post");
            // Assert
            Assert.That(entry, Is.Null);
        }

        [Test]
        public void RemoveCommand_Success()
        {
            // Arrange
            var command = "get";
            manager.AddCommand(command);
            // Act
            var entry = manager.RemoveCommand(command);
            // Assert
            Assert.That(entry, Is.Not.Null);
            Assert.That(command, Is.EqualTo(entry.GetCommand()));
        }

        [Test]
        public void RemoveCommand_DoesNotExist_IsNull()
        {
            // Arrange, Act
            var entry = manager.RemoveCommand("get");
            // Assert
            Assert.That(entry, Is.Null);
        }

        [Test]
        public void AddEndpoint_Success()
        {
            // Arrange
            var command = "get";
            manager.AddCommand(command);
            var endpoint = "/";
            // Act, Assert
            Assert.That(() => manager.AddEndpoint(command, endpoint, basicEndpointEntry), Throws.Nothing);
        }

        [Test]
        public void AddEndpoint_CommandDoesNotExist_ThrowsException()
        {
            // Arrange
            var command = "get";
            var endpoint = "/";
            // Act, Assert
            Assert.That(() => manager.AddEndpoint(command, endpoint, basicEndpointEntry), Throws.Exception.TypeOf<EndpointManagerException>());
        }

        [Test]
        public void AddEndpoint_GoodEndpoints_NoExceptions()
        {
            // Arrange
            var command = "get";
            var endpoints = new List<string>();
            // endpoint can be a single slash
            endpoints.Add("/");
            // endpopint can be composed of any alphanumerics as well as -.<>_/ characters
            // (but <> have to be enclosing something)
            string goodVariableStartCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
            string goodVariableCharacters = goodVariableStartCharacters + "1234567890";
            string goodCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-._";
            foreach (var character in goodCharacters)
            {
                endpoints.Add(character.ToString());
                endpoints.Add($"{character}/test");
                endpoints.Add($"test/{character}");
            }
            // endpoint can contain braces for variable portion or url; they ahve to enclose something
            endpoints.Add("good/<braces>");
            endpoints.Add("<braces>");
            endpoints.Add("<good>/braces");
            // variables can start with alphabet and underscore, non-first characters can be alphanumerics and underscore
            foreach (var character in goodVariableStartCharacters)
            {
                endpoints.Add($"<{character}>");
                foreach (var otherCharacter in goodVariableCharacters)
                {
                    endpoints.Add($"<{character.ToString() + otherCharacter.ToString()}>");
                }
            }
            // variable name can start with underscore
            endpoints.Add("<_underscore>/in/variable/name");
            // multiple variables allowed separated by slashes
            endpoints.Add("<multiple>/<variables>");
            // endpoint can start or end with multiple (or no) slashes
            endpoints.Add("no_slashes_at_all");
            endpoints.Add("/only_slash_at_start");
            endpoints.Add("only_slash_at_end/");
            endpoints.Add("/surrounding_slashes/");
            endpoints.Add("////multiple_slashes/////////////////");

            // Act, Assert
            foreach (var endpoint in endpoints)
            {
                // re-add command to make sure each endpoint is tested individually
                manager.AddCommand(command);
                Assert.That(() => manager.AddEndpoint(command, endpoint, basicEndpointEntry), Throws.Nothing);
                manager.RemoveCommand(command);
            }
        }

        [Test]
        public void AddEndpoint_BadEndpoints_ThrowsException()
        {
            // Arrange
            var command = "get";
            var endpoints = new List<string>();
            // endpoint cannot be blank
            endpoints.Add("");
            // non-alpha numeric or non -.<>_/ symbols are not allowed
            string badCharacters = "!@#$%^&*()=+`~[}{]\\|;:\"', ";
            foreach (var character in badCharacters)
            {
                endpoints.Add(character.ToString());
                endpoints.Add($"{character}/test");
                endpoints.Add($"test/{character}");
            }
            // consecutive slahses in the middle are not allowed
            endpoints.Add("bad//endpoint");
            endpoints.Add("/bad/endpoint//2/");
            // braces cannot be across a slash
            endpoints.Add("bad/<bra/ces>");
            // braces cannot have nothing in between
            endpoints.Add("bad/<>/braces");
            // braces must close
            endpoints.Add("unmatched/<braces");
            endpoints.Add("unmatched/<braces>>");
            endpoints.Add("unmatched/<braces>/other>");
            endpoints.Add(">braces");
            endpoints.Add("braces<");
            // braces cannot contain other braces
            endpoints.Add("unmatched/<<braces>>");
            endpoints.Add("unmatched/<b<race>s>");
            // braces cannot be placed directly adjacent to each other
            endpoints.Add("multiple/<unslashed><braces>");
            // braces cannot be placed more than once between slashes
            endpoints.Add("multiple/<braces>.<between>");
            endpoints.Add("multiple/<braces>.<between>/slashes");
            endpoints.Add("<bad>bad<braces>");
            // variable name in braces cannot start with a number
            endpoints.Add("starts/<1withnumber>");
            // multiple variables cannot have the same name
            endpoints.Add("<variable>/<variable>");

            // Act, Assert
            foreach (var endpoint in endpoints)
            {
                // re-add command to make sure each endpoint is tested individually
                manager.AddCommand(command);
                Assert.That(() => manager.AddEndpoint(command, endpoint, basicEndpointEntry), Throws.Exception.TypeOf<EndpointManagerException>());
                manager.RemoveCommand(command);
            }
        }

        [Test]
        public void AddEndpoint_EquivalentEndpoints_ThrowsException()
        {
            // Arrange
            var command = "get";
            var endpoints = new List<string>();
            // add valid endpoints
            manager.AddCommand(command);
            manager.AddEndpoint(command, "willalreadyexist", basicEndpointEntry);
            manager.AddEndpoint(command, "willalready/<exist>", basicEndpointEntry);
            // endpoint cannot already exist; slash at beginning or end makes no difference
            endpoints.Add("willalreadyexist");
            endpoints.Add("/willalreadyexist");
            endpoints.Add("willalreadyexist/");
            endpoints.Add("///willalreadyexist/////");
            // equivalent variable format is also not allowed
            endpoints.Add("willalready/<exist>");
            endpoints.Add("willalready/<exist1>");

            // Act, Assert
            foreach (var endpoint in endpoints)
            {
                Assert.That(() => manager.AddEndpoint(command, endpoint, basicEndpointEntry), Throws.Exception.TypeOf<EndpointManagerException>());
            }
        }

        [Test]
        public void GetEndpoint()
        {
            // Arrange
            var command = "get";
            var endpoint = "/";
            manager.AddCommand(command);
            manager.AddEndpoint(command, endpoint, basicEndpointEntry);

            // Act, Assert
            EndpointValue endpointValue = null;
            Assert.That(() => endpointValue = manager.GetEndpoint(command, endpoint), Throws.Nothing);
            Assert.That(endpointValue, Is.Not.Null);
            // variable map should be empty
            Assert.That(endpointValue.GetValues(), Is.Empty);
            // entry should be the same as put in
            Assert.That(endpointValue.GetEntry(), Is.SameAs(basicEndpointEntry));
        }

        [Test]
        public void GetEndpoint_WithVariables()
        {
            // Arrange
            var command = "get";
            manager.AddCommand(command);
            var regex = new Regex("@", RegexOptions.Compiled);
            var endpointTemplates = new List<string>()
            {
                "test/@",
                "@/@",
                "test/@/@",
                "test/@/other/@",
                "@/tests/variable0/@",
                "@/@/@/@/@",
                "@/@/@/@/@/test"
            };
            // Act, Assert
            foreach (var endpointTemplate in endpointTemplates)
            {
                var count = 0;
                var variableMap = new Dictionary<string, string>();
                var preparedTemplate = endpointTemplate;
                var preparedQuery = endpointTemplate;
                var match = regex.Match(endpointTemplate);
                while (match.Success)
                {
                    var name = $"variable{count}";
                    var value = $"value{count}";
                    variableMap.Add(name, value);
                    preparedTemplate = regex.Replace(preparedTemplate, $"<{name}>", 1);
                    preparedQuery = regex.Replace(preparedQuery, value, 1);
                    match = match.NextMatch();
                    count++;
                }
                // add endpoint
                manager.AddEndpoint(command, preparedTemplate, basicEndpointEntry);
                // get endpoint
                var endpointValue = manager.GetEndpoint(command, preparedQuery);
                // Assert
                Assert.That(endpointValue, Is.Not.Null);
                // returned values should have same count as template variables
                Assert.That(endpointValue.GetValues().Count, Is.EqualTo(variableMap.Count));
                foreach(var pair in variableMap)
                {
                    var variable = pair.Key;
                    Assert.That(endpointValue.GetValues(), Contains.Key(variable), $"Expected variable '{variable}' not found");
                    var expectedValue = pair.Value;
                    var actualValue = endpointValue.GetValues().GetValueOrDefault(variable);
                    Assert.That(actualValue, Is.EqualTo(expectedValue), $"Variable '{variable}' had value '{actualValue}' instead of '{expectedValue}");
                }
            }
        }

        [Test]
        public void GetEndpoint_DoesNotExist_ThrowsException()
        {
            // Arrange
            var command = "get";
            manager.AddCommand(command);
            var endpoints = new List<string>();
            // can begin/end with 0 or many slashes
            endpoints.Add("no_slashes_at_all");
            endpoints.Add("/only_slash_at_start");
            endpoints.Add("only_slash_at_end/");
            endpoints.Add("/surrounding_slashes/");
            endpoints.Add("////multiple_slashes/////////////////");
            // endpoints can contain regex-life format, causing no issues
            endpoints.Add("^[a-zA-Z0-9]+$");

            // Act, Assert
            foreach (var endpoint in endpoints)
            {
                Assert.That(() => manager.GetEndpoint(command, endpoint), Throws.Exception.TypeOf<EndpointManagerException>());
            }
        }

        [Test]
        public void GetEndpoint_CommandDoesNotExist_ThrowsException()
        {
            // Arrange, Act, Assert
            Assert.That(() => manager.GetEndpoint("get", "/"), Throws.Exception.TypeOf<EndpointManagerException>());
        }

        [Test]
        public void RemoveEndpoint()
        {
            // Arrange
            var command = "get";
            var endpoint = "/";
            manager.AddCommand(command);
            manager.AddEndpoint(command, endpoint, basicEndpointEntry);
            // Act, Assert
            var removed = manager.RemoveEndpoint(command, endpoint);
            Assert.That(removed, Is.Not.Null);
            Assert.That(removed.GetEntry(), Is.SameAs(basicEndpointEntry));
        }


    }
}
