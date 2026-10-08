using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using MimeKit;

namespace FluentEmail.AmazonSES
{
    /// <summary>
    /// Send emails with Amazon Simple Email Service using the SESv2 API.
    /// Messages go out as raw MIME so attachments, headers and priority
    /// survive the trip exactly as built.
    /// Implements https://github.com/lukencode/FluentEmail/issues/181
    /// </summary>
    public class SesSender : ISender
    {
        private readonly SesSenderOptions _options;
        private IAmazonSimpleEmailServiceV2 _client;

        /// <summary>
        /// Creates a sender that builds its own SESv2 client from <paramref name="options"/>.
        /// When no explicit keys are given the AWS SDK default credential chain is used.
        /// </summary>
        public SesSender(SesSenderOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Creates a sender around an existing SESv2 client (useful for DI and tests).
        /// The client is not disposed by this sender.
        /// </summary>
        public SesSender(IAmazonSimpleEmailServiceV2 client, SesSenderOptions options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Lazily created SESv2 client. Override to supply a custom client.
        /// </summary>
        protected virtual IAmazonSimpleEmailServiceV2 Client
            => _client ?? (_client = CreateClient());

        /// <summary>
        /// Send the specified email.
        /// </summary>
        public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
        {
            return SendAsync(email, token).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Send the specified email.
        /// </summary>
        public async Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null)
        {
            var response = new SendResponse();

            try
            {
                var message = CreateMailMessage(email);

                using (var stream = new MemoryStream())
                {
                    await message.WriteToAsync(stream, token.GetValueOrDefault()).ConfigureAwait(false);
                    stream.Position = 0;

                    var request = CreateSendEmailRequest(email, stream);
                    var sesResponse = await SendEmailViaSesAsync(request, token.GetValueOrDefault()).ConfigureAwait(false);
                    response.MessageId = sesResponse.MessageId;
                }
            }
            catch (Exception ex)
            {
                response.ErrorMessages.Add(ex.Message);
            }

            return response;
        }

        /// <summary>
        /// Builds the SESv2 client from the configured options.
        /// </summary>
        protected virtual IAmazonSimpleEmailServiceV2 CreateClient()
        {
            var config = new AmazonSimpleEmailServiceV2Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region)
            };

            if (!string.IsNullOrEmpty(_options.AccessKey))
                return new AmazonSimpleEmailServiceV2Client(
                    new BasicAWSCredentials(_options.AccessKey, _options.SecretKey), config);

            return new AmazonSimpleEmailServiceV2Client(config);
        }

        /// <summary>
        /// Sends the request through SES. Override in tests to capture the request
        /// without touching AWS.
        /// </summary>
        protected virtual Task<SendEmailResponse> SendEmailViaSesAsync(SendEmailRequest request, CancellationToken cancellationToken)
            => Client.SendEmailAsync(request, cancellationToken);

        /// <summary>
        /// Builds the SESv2 SendEmail request carrying the message as raw MIME.
        /// </summary>
        protected virtual SendEmailRequest CreateSendEmailRequest(IFluentEmail email, MemoryStream rawMessage)
        {
            var request = new SendEmailRequest
            {
                FromEmailAddress = email.Data.FromAddress.EmailAddress,
                Content = new EmailContent
                {
                    Raw = new RawMessage { Data = rawMessage }
                }
            };

            if (!string.IsNullOrEmpty(_options.ConfigurationSetName))
                request.ConfigurationSetName = _options.ConfigurationSetName;

            return request;
        }

        /// <summary>
        /// Create a MimeMessage from the email data. Virtual so subclasses can
        /// preprocess the message before sending (e.g. DKIM-sign it).
        /// </summary>
        protected virtual MimeMessage CreateMailMessage(IFluentEmail email)
        {
            var data = email.Data;

            var message = new MimeMessage();

            if (!message.Headers.Contains(HeaderId.Subject))
                message.Headers.Add(HeaderId.Subject, Encoding.UTF8, data.Subject ?? string.Empty);
            else
                message.Headers[HeaderId.Subject] = data.Subject ?? string.Empty;

            message.Headers.Add(HeaderId.Encoding, Encoding.UTF8.EncodingName);

            message.From.Add(new MailboxAddress(data.FromAddress.Name, data.FromAddress.EmailAddress));

            var builder = new BodyBuilder();
            if (!string.IsNullOrEmpty(data.PlaintextAlternativeBody))
            {
                builder.TextBody = data.PlaintextAlternativeBody;
                builder.HtmlBody = data.Body;
            }
            else if (!data.IsHtml)
            {
                builder.TextBody = data.Body;
            }
            else
            {
                builder.HtmlBody = data.Body;
            }

            foreach (var attachment in data.Attachments)
            {
                var mimeAttachment = builder.Attachments.Add(
                    attachment.Filename, attachment.Data, ContentType.Parse(attachment.ContentType));
                mimeAttachment.ContentId = attachment.ContentId;
            }

            message.Body = builder.ToMessageBody();

            foreach (var header in data.Headers)
            {
                message.Headers.Add(header.Key, header.Value);
            }

            foreach (var address in data.ToAddresses)
            {
                message.To.Add(new MailboxAddress(address.Name, address.EmailAddress));
            }

            foreach (var address in data.CcAddresses)
            {
                message.Cc.Add(new MailboxAddress(address.Name, address.EmailAddress));
            }

            foreach (var address in data.BccAddresses)
            {
                message.Bcc.Add(new MailboxAddress(address.Name, address.EmailAddress));
            }

            foreach (var address in data.ReplyToAddresses)
            {
                message.ReplyTo.Add(new MailboxAddress(address.Name, address.EmailAddress));
            }

            switch (data.Priority)
            {
                case Priority.Low:
                    message.Priority = MessagePriority.NonUrgent;
                    break;
                case Priority.Normal:
                    message.Priority = MessagePriority.Normal;
                    break;
                case Priority.High:
                    message.Priority = MessagePriority.Urgent;
                    break;
            }

            return message;
        }
    }
}
