// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace VirtualAssistant.Responses.Main
{
    public class MainResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                {
                    ResponseIds.Cancelled,
                    (context, data) =>
                    MessageFactory.Text(
                        text: MainStrings.CANCELLED,
                        ssml: MainStrings.CANCELLED,
                        inputHint: InputHints.AcceptingInput)
                },
                {
                    ResponseIds.Completed,
                    (context, data) =>
                        BuildCompleteDialog(context, data)
                },
                {
                    ResponseIds.Confused,
                    (context, data) =>
                    MessageFactory.Text(
                        text: MainStrings.CONFUSED,
                        ssml: MainStrings.CONFUSED,
                        inputHint: InputHints.AcceptingInput)
                },
                {
                    ResponseIds.Greeting,
                    (context, data) =>
                    BuildGreetingCard(context, data)
                },
                { ResponseIds.Help, (context, data) => BuildHelpCard(context, data) },
                { ResponseIds.NewUserGreeting, (context, data) => BuildNewUserGreetingCard(context, data) },
                { ResponseIds.ReturningUserGreeting, (context, data) => BuildReturningUserGreetingCard(context, data) },
            }
        };

        public MainResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public static IMessageActivity BuildCompleteDialog(ITurnContext context, dynamic data)
        {
            var reply = MessageFactory.Text("What else can I help you with?");
            reply.Type = ActivityTypes.Message;
            reply.TextFormat = TextFormatTypes.Plain;

            if (data.Classification == "student")
            {
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                {
                    new CardAction(){ Title = "Edit Profile", Type=ActionTypes.ImBack, Value="Edit my profile." },
                    new CardAction(){ Title = "Search", Type=ActionTypes.ImBack, Value="How do I perform a search?" },
                    }
                };
            }

            else
            {
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                {
                    new CardAction(){ Title = "Edit Profile", Type=ActionTypes.ImBack, Value="Edit my profile." },
                    new CardAction(){ Title = "Update Availability", Type=ActionTypes.ImBack, Value="Update my availability." },

                }
                };
            }
            

            return reply;
        }

        public static IMessageActivity BuildGreetingCard(ITurnContext turnContext, dynamic data)
        {
            var introCard = File.ReadAllText(MainStrings.GREETING_CARD);
            var card = AdaptiveCard.FromJson(introCard).Card;
            var attachment = new Attachment(AdaptiveCard.ContentType, content: card);

            var response = MessageFactory.Attachment(attachment, ssml: card.Speak, inputHint: InputHints.IgnoringInput);

            //response.SuggestedActions = new SuggestedActions
            //{
            //    Actions = new List<CardAction>()
            //    {
            //        new CardAction(type: ActionTypes.ImBack, title: MainStrings.GREETING_BTN_1, value: MainStrings.GREETING_BTN_1_VALUE),
            //        new CardAction(type: ActionTypes.ImBack, title: MainStrings.GREETING_BTN_2, value: MainStrings.GREETING_BTN_2_VALUE),
            //        new CardAction(type: ActionTypes.ImBack, title: MainStrings.GREETING_BTN_3, value: MainStrings.GREETING_BTN_3_VALUE),
            //    },
            //};

            return response;
        }

        public static IMessageActivity BuildNewUserGreetingCard(ITurnContext turnContext, dynamic data)
        {
            var introCard = File.ReadAllText(MainStrings.INTRO_PATH);
            var card = AdaptiveCard.FromJson(introCard).Card;
            var attachment = new Attachment(AdaptiveCard.ContentType, content: card);

            

            var response = MessageFactory.Attachment(attachment, ssml: card.Speak, inputHint: InputHints.ExpectingInput);


            

            response.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.HELP_BTN_TEXT_1, value: MainStrings.HELP_BTN_VALUE_1),
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.HELP_BTN_TEXT_2, value: MainStrings.HELP_BTN_VALUE_2),
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.HELP_BTN_TEXT_3, value: MainStrings.HELP_BTN_VALUE_3),
                },
            };

            return response;
        }

        public static IMessageActivity BuildReturningUserGreetingCard(ITurnContext turnContext, dynamic data)
        {
            var introCard = File.ReadAllText(MainStrings.INTRO_RETURNING);
            var card = AdaptiveCard.FromJson(introCard).Card;
            var attachment = new Attachment(AdaptiveCard.ContentType, content: card);

            var response = MessageFactory.Attachment(attachment, ssml: card.Speak, inputHint: InputHints.AcceptingInput);

            response.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.HELP_BTN_TEXT_1, value: MainStrings.HELP_BTN_VALUE_1),
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.HELP_BTN_TEXT_2, value: MainStrings.HELP_BTN_VALUE_2),
                    new CardAction(type: ActionTypes.OpenUrl, title: MainStrings.HELP_BTN_TEXT_3, value: MainStrings.HELP_BTN_VALUE_3),
                },
            };

            return response;
        }

        public static IMessageActivity BuildHelpCard(ITurnContext turnContext, dynamic data)
        {
            var attachment = new HeroCard()
            {
                Title = MainStrings.HELP_TITLE,
                Text = MainStrings.HELP_TEXT,
            }.ToAttachment();

            var response = MessageFactory.Attachment(attachment, ssml: MainStrings.HELP_TEXT, inputHint: InputHints.IgnoringInput);

            response.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.HELP_BTN_TEXT_1, value: MainStrings.HELP_BTN_VALUE_1),
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.HELP_BTN_TEXT_2, value: MainStrings.HELP_BTN_VALUE_2),
                    new CardAction(type: ActionTypes.ImBack, title: MainStrings.HELP_BTN_TEXT_3, value: MainStrings.HELP_BTN_VALUE_3),
                },
            };

            return response;
        }

        public class ResponseIds
        {
            // Constants
            public const string Cancelled = "cancelled";
            public const string Completed = "completed";
            public const string Confused = "confused";
            public const string Greeting = "greeting";
            public const string Help = "help";
            public const string NewUserGreeting = "newUser";
            public const string ReturningUserGreeting = "returningUser";
        }
    }
}
