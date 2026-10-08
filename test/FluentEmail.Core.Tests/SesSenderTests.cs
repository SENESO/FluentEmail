using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleEmailV2.Model;
using FluentEmail.AmazonSES;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using MimeKit;
using NUnit.Framework;
using Attachment = FluentEmail.Core.Models.Attachment;

namespace FluentEmail.AmazonSES.Tests
{
    /// <summary>
    /// Tests for the Amazon SES sender (upstream issue #181).
    /// None of these touch AWS: the SES transport is captured by a test subclass.
    /// </summary>
    public class SesSenderTests
    {
        const string toEmail = "bob@test.com";
        const string fromEmail = "johno@test.com";
        const string subject = "ses test";
        const string body = "<h2>Hello via SES</h2>";

        [Test]
        public void CreateMailMessage_BuildsExpectedMimeMessage()
        {
            var sender = new TestableSesSender();

            var email = Email
                .From(fromEmail, "John O")
                .To(toEmail, "Bob")
                .Subject(subject)
                .Body(body, true);

            var message = sender.BuildMessage(email);

            Assert.AreEqual(subject, message.Subject);
            Assert.AreEqual(fromEmail, message.From.Mailboxes.Single().Address);
            Assert.AreEqual("John O", message.From.Mailboxes.Single().Name);
            Assert.AreEqual(toEmail, message.To.Mailboxes.Single().Address);
            Assert.IsTrue(message.HtmlBody.Contains("Hello via SES"));
        }

        [Test]
        public async Task SendAsync_SendsRawMimeThroughSesAndMapsMessageId()
        {
            var sender = new CapturingSesSender();

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine("attachment content");
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            var email = Email
                .From(fromEmail)
                .To(toEmail)
                .Subject(subject)
                .Body(body, true)
                .Attach(new Attachment
                {
                    Data = stream,
                    ContentType = "text/plain",
                    Filename = "notes.txt"
                });

            var response = await sender.SendAsync(email);

            Assert.IsTrue(response.Successful);
            Assert.AreEqual("ses-message-id-123", response.MessageId);

            var request = sender.CapturedRequest;
            Assert.IsNotNull(request);
            Assert.AreEqual(fromEmail, request.FromEmailAddress);

            request.Content.Raw.Data.Position = 0;
            var raw = new StreamReader(request.Content.Raw.Data).ReadToEnd();
            Assert.IsTrue(raw.Contains(subject), "Raw MIME should contain the subject.");
            Assert.IsTrue(raw.Contains("Hello via SES"), "Raw MIME should contain the body.");
            Assert.IsTrue(raw.Contains("notes.txt"), "Raw MIME should contain the attachment.");
        }

        [Test]
        public async Task SendAsync_MapsSesErrorsToResponse()
        {
            var sender = new CapturingSesSender
            {
                Throw = new InvalidOperationException("SES is down")
            };

            var email = Email
                .From(fromEmail)
                .To(toEmail)
                .Subject(subject)
                .Body(body);

            var response = await sender.SendAsync(email);

            Assert.IsFalse(response.Successful);
            Assert.IsTrue(response.ErrorMessages.Any(m => m.Contains("SES is down")));
        }

        private class TestableSesSender : SesSender
        {
            public TestableSesSender() : base(new SesSenderOptions { Region = "us-east-1" }) { }

            public MimeMessage BuildMessage(IFluentEmail email) => CreateMailMessage(email);
        }

        private class CapturingSesSender : SesSender
        {
            public SendEmailRequest CapturedRequest { get; private set; }
            public Exception Throw { get; set; }

            public CapturingSesSender() : base(new SesSenderOptions { Region = "us-east-1" }) { }

            protected override Task<SendEmailResponse> SendEmailViaSesAsync(SendEmailRequest request, CancellationToken cancellationToken)
            {
                if (Throw != null) throw Throw;
                CapturedRequest = request;
                return Task.FromResult(new SendEmailResponse { MessageId = "ses-message-id-123" });
            }
        }
    }
}
