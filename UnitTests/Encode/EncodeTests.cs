using Ceptic.Encode;
using Ceptic.Encode.Encoders;
using Ceptic.Encode.Exceptions;
using NUnit.Framework;
using System.Text;

namespace UnitTests.Encode
{
    public class EncodeTests
    {
        [Test]
        public void EncodeAndDecodeBase64()
        {
            // Arrange
            var input = "someTestString123!@#";
            var encoder = new EncodeBase64();
            // Act
            var encoded = encoder.Encode(Encoding.UTF8.GetBytes(input));
            var decoded = encoder.Decode(encoded);
            var encodedString = Encoding.UTF8.GetString(encoded);
            var output = Encoding.UTF8.GetString(decoded);
            // Assert
            Assert.That(input, Is.EqualTo(output));
            Assert.That(input, Is.Not.EqualTo(encodedString));
            Assert.That(encoded, Is.Not.EqualTo(decoded));
        }

        [Test]
        public void EncodeAndDecodeGzip()
        {
            // Arrange
            var input = "someTestString123!@#";
            var encoder = new EncodeGzip();
            // Act
            var encoded = encoder.Encode(Encoding.UTF8.GetBytes(input));
            var decoded = encoder.Decode(encoded);
            var encodedString = Encoding.UTF8.GetString(encoded);
            var output = Encoding.UTF8.GetString(decoded);
            // Assert
            Assert.That(input, Is.EqualTo(output));
            Assert.That(input, Is.Not.EqualTo(encodedString));
            Assert.That(encoded, Is.Not.EqualTo(decoded));
        }

        [Test]
        public void EncodeAndDecodeNone()
        {
            // Arrange
            var input = "someTestString123!@#";
            var encoder = new EncodeNone();
            // Act
            var encoded = encoder.Encode(Encoding.UTF8.GetBytes(input));
            var decoded = encoder.Decode(encoded);
            var encodedString = Encoding.UTF8.GetString(encoded);
            var output = Encoding.UTF8.GetString(decoded);
            // Assert
            Assert.That(input, Is.EqualTo(output));
            Assert.That(input, Is.EqualTo(encodedString));
            Assert.That(encoded, Is.EqualTo(decoded));
        }

        [Test]
        public void EncodeGetter_String_Base64()
        {
            // Arrange
            var input = "someTestString123!@#";
            var encodings = EncodeType.Base64.GetValue();
            // Act
            var handler = EncodeGetter.Get(encodings);
            var encoded = handler.Encode(Encoding.UTF8.GetBytes(input));
            var decoded = handler.Decode(encoded);
            var encodedString = Encoding.UTF8.GetString(encoded);
            var output = Encoding.UTF8.GetString(decoded);
            // Assert
            Assert.That(input, Is.EqualTo(output));
            Assert.That(input, Is.Not.EqualTo(encodedString));
            Assert.That(encoded, Is.Not.EqualTo(decoded));
        }

        [Test]
        public void EncodeGetter_String_Base64_Gzip()
        {
            // Arrange
            var input = "someTestString123!@#";
            var expectedEncoded = new EncodeGzip().Encode(new EncodeBase64().Encode(Encoding.UTF8.GetBytes(input)));
            var expectedDecoded = new EncodeBase64().Decode(new EncodeGzip().Decode(expectedEncoded));
            var encodings = $"{EncodeType.Base64.GetValue()},{EncodeType.Gzip.GetValue()}";
            // Act
            var handler = EncodeGetter.Get(encodings);
            var encoded = handler.Encode(Encoding.UTF8.GetBytes(input));
            var decoded = handler.Decode(encoded);
            var encodedString = Encoding.UTF8.GetString(encoded);
            var output = Encoding.UTF8.GetString(decoded);
            // Assert
            Assert.That(input, Is.EqualTo(output));
            Assert.That(input, Is.Not.EqualTo(encodedString));
            Assert.That(encoded, Is.Not.EqualTo(decoded));
            Assert.That(expectedEncoded, Is.EqualTo(encoded));
            Assert.That(expectedDecoded, Is.EqualTo(decoded));
        }

        [Test]
        public void EncodeGetter_InvalidEncoding_UnknownEncodingException()
        {
            // Arrange, Act, Assert
            Assert.That(() => EncodeGetter.Get("unknown"), Throws.Exception.TypeOf<UnknownEncodingException>());
        }

        [Test]
        public void EncodeGetter_Null_EncodeNone()
        {
            // Arrange
            var input = "someTestString123!@#";
            // Act
            var handler = EncodeGetter.Get(EncodeType.None.GetValue());
            var encoded = handler.Encode(Encoding.UTF8.GetBytes(input));
            var decoded = handler.Decode(encoded);
            var encodedString = Encoding.UTF8.GetString(encoded);
            var output = Encoding.UTF8.GetString(decoded);
            // Assert
            Assert.That(input, Is.EqualTo(output));
            Assert.That(input, Is.EqualTo(encodedString));
            Assert.That(encoded, Is.EqualTo(decoded));
        }
    }
}
