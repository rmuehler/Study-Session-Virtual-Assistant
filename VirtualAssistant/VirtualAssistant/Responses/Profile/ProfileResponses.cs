// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using VirtualAssistant.Models;

namespace VirtualAssistant.Responses.Profile
{

    public class ProfileResponses : TemplateManager
    {

        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary

        {
            ["default"] = new TemplateIdMap
            {
                {
                    ResponseIds.Cancelled,
                    (context, data) =>
                    MessageFactory.Text(
                        text: ProfileStrings.CANCELLED,
                        ssml: ProfileStrings.CANCELLED,
                        inputHint: InputHints.AcceptingInput)
                },
                {
                    ResponseIds.Completed,
                    (context, data) =>
                    MessageFactory.Text(
                        text: ProfileStrings.COMPLETED,
                        ssml: ProfileStrings.COMPLETED,
                        inputHint: InputHints.AcceptingInput)
                },
                {
                    ResponseIds.Confused,
                    (context, data) =>
                    MessageFactory.Text(
                        text: ProfileStrings.CONFUSED,
                        ssml: ProfileStrings.CONFUSED,
                        inputHint: InputHints.AcceptingInput)
                },
                {
                    ResponseIds.Greeting,
                    (context, data) =>
                    MessageFactory.Text(
                        text: ProfileStrings.GREETING,
                        ssml: ProfileStrings.GREETING,
                        inputHint: InputHints.AcceptingInput)
                },
                {
                    ResponseIds.EditSuccess,
                    (context, data) =>
                    MessageFactory.Text(
                        text: ProfileStrings.EDIT_SUCCESS,
                        ssml: ProfileStrings.EDIT_SUCCESS,
                        inputHint: InputHints.AcceptingInput)
                },
                { ResponseIds.Help, (context, data) => BuildHelpCard(context, data) },
                { ResponseIds.ShowStudentProfile, (context, data) => BuildStudentCardAsync(context, data) },
                { ResponseIds.ShowTutorProfile, (context, data) => BuildTutorCard(context, data) },
                { ResponseIds.UpdateAvailability, (context, data) => BuildAvailabilityCard(context, data) },

            }
        };

        public ProfileResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public static IMessageActivity BuildStudentCardAsync(ITurnContext turnContext, dynamic data)
        {
            var introCard = File.ReadAllText(ProfileStrings.STUDENTPROFILE_PATH);
            introCard = introCard.Replace("Placeholder Name", data.Name);
            introCard = introCard.Replace("Placeholder Email", data.EmailAdress);
            var card = AdaptiveCard.FromJson(introCard).Card;
            var attachment = new Attachment(AdaptiveCard.ContentType, content: card);
            var response = MessageFactory.Attachment(attachment, ssml: card.Speak, inputHint: InputHints.IgnoringInput);
            response.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.MessageBack, title: ProfileStrings.HELP_BTN_TEXT_1, value: ProfileStrings.HELP_BTN_VALUE_1),
                    new CardAction(type: ActionTypes.MessageBack, title: ProfileStrings.HELP_BTN_TEXT_2, value: ProfileStrings.HELP_BTN_VALUE_2),
                    new CardAction(type: ActionTypes.MessageBack, title: ProfileStrings.HELP_BTN_TEXT_3, value: ProfileStrings.HELP_BTN_VALUE_3),
                },
            };

            return response;
        }

        public static IMessageActivity BuildTutorCard(ITurnContext turnContext, dynamic data)
        {
            var introCard = File.ReadAllText(ProfileStrings.TUTORPROFILE_PATH);
            var card = AdaptiveCard.FromJson(introCard).Card;
            var attachment = new Attachment(AdaptiveCard.ContentType, content: card);
            introCard = introCard.Replace("Placeholder Name", data.Name);
            introCard = introCard.Replace("Placeholder Email", data.EmailAdress);
            var response = MessageFactory.Attachment(attachment, ssml: card.Speak, inputHint: InputHints.AcceptingInput);
            return response;
        }

        public static IMessageActivity BuildAvailabilityCard(ITurnContext turnContext, dynamic data)
        {
            var introCard = File.ReadAllText(ProfileStrings.UPDATEAVAILABILITY_PATH);
            var card = AdaptiveCard.FromJson(introCard).Card;
            var attachment = new Attachment(AdaptiveCard.ContentType, content: card);
            var response = MessageFactory.Attachment(attachment, ssml: card.Speak, inputHint: InputHints.AcceptingInput);
            return response;
        }

        public static IMessageActivity BuildHelpCard(ITurnContext turnContext, dynamic data)
        {
            var attachment = new HeroCard()
            {
                Title = ProfileStrings.HELP_TITLE,
                Text = ProfileStrings.HELP_TEXT,
            }.ToAttachment();

            var response = MessageFactory.Attachment(attachment, ssml: ProfileStrings.HELP_TEXT, inputHint: InputHints.IgnoringInput);



            response.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.ImBack, title: ProfileStrings.HELP_BTN_TEXT_1, value: ProfileStrings.HELP_BTN_VALUE_1),
                    new CardAction(type: ActionTypes.ImBack, title: ProfileStrings.HELP_BTN_TEXT_2, value: ProfileStrings.HELP_BTN_VALUE_2),
                    new CardAction(type: ActionTypes.ImBack, title: ProfileStrings.HELP_BTN_TEXT_3, value: ProfileStrings.HELP_BTN_VALUE_3),
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
            public const string ShowStudentProfile = "studentProfile";
            public const string ShowTutorProfile = "tutorProfile";
            public const string EditSuccess = "editSuccess";
            public const string UpdateAvailability = "updateAvailability";

        }
    }
}
