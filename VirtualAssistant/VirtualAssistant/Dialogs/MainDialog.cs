// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Feedback;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using VirtualAssistant.Models;
using VirtualAssistant.Responses.Cancel;
using VirtualAssistant.Responses.Main;
using VirtualAssistant.Services;

namespace VirtualAssistant.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private MainResponses _responder = new MainResponses();
        private IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        private IStatePropertyAccessor<OnboardingState> _onboardingState;
        private IStatePropertyAccessor<SkillContext> _skillContextAccessor;
        private OnboardingState _state;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            OnboardingDialog onboardingDialog,
            EscalateDialog escalateDialog,
            CancelDialog cancelDialog,
            Edit_Profile editProfile,
            Search_by_Subject searchSubject,
            Search_by_Tutor searchtutor,
            Greeting_Dialog greeting_Dialog,
            SubmitDialog submitDialog,
            Update_Availability updateAvailability,
            List<SkillDialog> skillDialogs,
            IBotTelemetryClient telemetryClient,
            UserState userState)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            TelemetryClient = telemetryClient;
            _onboardingState = userState.CreateProperty<OnboardingState>(nameof(OnboardingState));
            _skillContextAccessor = userState.CreateProperty<SkillContext>(nameof(SkillContext));
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");

            AddDialog(onboardingDialog);
            AddDialog(escalateDialog);
            AddDialog(cancelDialog);
            AddDialog(editProfile);
            AddDialog(searchSubject);
            AddDialog(greeting_Dialog);
            AddDialog(searchtutor);
            AddDialog(submitDialog);
            AddDialog(updateAvailability);


            foreach (var skillDialog in skillDialogs)
            {
                AddDialog(skillDialog);
            }
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var view = new MainResponses();
            var onboardingState = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());
            _state = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());
            var name = _state.ConfuseCounter = 0;

            // await dc.BeginDialogAsync(nameof(Update_Availability));
            //Task s = view.ReplyWith(dc.Context, MainResponses.ResponseIds.Greeting);
            DialogTurnResult re = await dc.BeginDialogAsync(nameof(Greeting_Dialog));
            var self =  _state.self = (User)re.Result;
            await _onboardingState.SetAsync(dc.Context, _state, cancellationToken);
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get cognitive models for locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var cognitiveModels = _services.CognitiveModelSets[locale];

            // Check dispatch result
            var dispatchResult = await cognitiveModels.DispatchService.RecognizeAsync<DispatchLuis>(dc.Context, CancellationToken.None);
            var intent = dispatchResult.TopIntent().intent;
            var test = dispatchResult.Entities;
            Console.WriteLine(test.ToString());

            var onboardingState = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());

            _state = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());
            User self = _state.self;

            // Identify if the dispatch intent matches any Action within a Skill if so, we pass to the appropriate SkillDialog to hand-off
            var identifiedSkill = SkillRouter.IsSkill(_settings.Skills, intent.ToString());

            if (identifiedSkill != null)
            {
                // We have identiifed a skill so initialize the skill connection with the target skill
                Console.WriteLine("Found a skill! " + intent.ToString());
                var result = await dc.BeginDialogAsync(identifiedSkill.Id);

                if (result.Status == DialogTurnStatus.Complete)
                {
                    await CompleteAsync(dc);
                }
            }

            else if (intent == DispatchLuis.Intent.l_General)
            {
                // If dispatch result is General luis model
                cognitiveModels.LuisServices.TryGetValue("General", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The General LUIS Model could not be found in your Bot Services configuration.");
                }
                else
                {
                    var result = await luisService.RecognizeAsync<GeneralLuis>(dc.Context, CancellationToken.None);

                    var generalIntent = result?.TopIntent().intent;

                    // switch on general intents
                    switch (generalIntent)
                    {
                        case GeneralLuis.Intent.Escalate:
                            {
                                // start escalate dialog
                                await dc.BeginDialogAsync(nameof(EscalateDialog));
                                break;
                            }

                        case GeneralLuis.Intent.None:
                        default:
                            {
                                // No intent was identified, send confused message
                                _state = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());
                                var name = _state.ConfuseCounter++;
                                await _onboardingState.SetAsync(dc.Context, _state, cancellationToken);
                                await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Confused);
                                break;
                            }
                    }
                }
            }
           else if (intent == DispatchLuis.Intent.l_searchskill)
            {
                // If dispatch result is General luis model
                cognitiveModels.LuisServices.TryGetValue("searchskill", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The General LUIS Model could not be found in your Bot Services configuration.");
                }
                else
                {
                    var result = await luisService.RecognizeAsync<searchskillLuis>(dc.Context, CancellationToken.None);

                    // to pass in BeginDialogAsync
                    Luis.searchskillLuis._Entities entities = result.Entities;
                    var searchIntent = result?.TopIntent().intent;
                    
                    // switch on general intents
                    switch (searchIntent)
                    {
                        case searchskillLuis.Intent.Search_by_Subject:
                            {
                                // start escalate dialog
                                await dc.BeginDialogAsync(nameof(Search_by_Subject), entities);
                                break;
                            }

                        case searchskillLuis.Intent.Search_by_Tutor:
                            {
                                await dc.BeginDialogAsync(nameof(Search_by_Tutor), entities);
                                break;
                            }
                        case searchskillLuis.Intent.None:
                        default:
                            {
                                // No intent was identified, send confused message
                                _state = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());
                                var name = _state.ConfuseCounter++;
                                await _onboardingState.SetAsync(dc.Context, _state, cancellationToken);
                                await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Confused);
                                break;
                            }
                    }
                }
            }
            else if (intent == DispatchLuis.Intent.l_profileskill)
            {
                // If dispatch result is General luis model
                cognitiveModels.LuisServices.TryGetValue("profileskill", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The General LUIS Model could not be found in your Bot Services configuration.");
                }
                else
                {
                    var result = await luisService.RecognizeAsync<profileskillLuis>(dc.Context, CancellationToken.None);

                    var profileIntent = result?.TopIntent().intent;

                    // switch on general intents
                    switch (profileIntent)
                    {
                        case profileskillLuis.Intent.Edit_Profile:
                            {
                                // start escalate dialog
                                await dc.BeginDialogAsync(nameof(Edit_Profile));
                                break;
                            }

                        case profileskillLuis.Intent.Update_Availability:
                            {
                                // start escalate dialog
                                await dc.BeginDialogAsync(nameof(Update_Availability));
                                break;
                            }

                        case profileskillLuis.Intent.None:
                        default:
                            {
                                // No intent was identified, send confused message
                                _state = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());
                                var name = _state.ConfuseCounter++;
                                await _onboardingState.SetAsync(dc.Context, _state, cancellationToken);
                                await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Confused);
                                break;
                            }
                    }
                }
            }
            else if (intent == DispatchLuis.Intent.q_Faq)
            {
                cognitiveModels.QnAServices.TryGetValue("Faq", out var qnaService);

                if (qnaService == null)
                {
                    throw new Exception("The specified QnA Maker Service could not be found in your Bot Services configuration.");
                }
                else
                {
                    var answers = await qnaService.GetAnswersAsync(dc.Context, null, null);

                    if (answers != null && answers.Count() > 0)
                    {
                        await dc.Context.SendActivityAsync(answers[0].Answer, speak: answers[0].Answer);
                    }
                    else
                    {
                        _state = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());
                        var name = _state.ConfuseCounter++;
                        await _onboardingState.SetAsync(dc.Context, _state, cancellationToken);
                        await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Confused);
                    }
                }
            }
            else if (intent == DispatchLuis.Intent.q_Chitchat)
            {
                cognitiveModels.QnAServices.TryGetValue("Chitchat", out var qnaService);

                if (qnaService == null)
                {
                    throw new Exception("The specified QnA Maker Service could not be found in your Bot Services configuration.");
                }
                else
                {
                    var answers = await qnaService.GetAnswersAsync(dc.Context, null, null);

                    if (answers != null && answers.Count() > 0)
                    {
                        await dc.Context.SendActivityAsync(answers[0].Answer, speak: answers[0].Answer);
                    }
                    else
                    {
                        _state = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());
                        var name = _state.ConfuseCounter++;
                        await _onboardingState.SetAsync(dc.Context, _state, cancellationToken);
                        await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Confused);
                    }
                }
            }
            else
            {
                // If dispatch intent does not map to configured models, send "confused" response.
                // Alternatively as a form of backup you can try QnAMaker for anything not understood by dispatch.
                _state = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());
                var name = _state.ConfuseCounter ++;
                await _onboardingState.SetAsync(dc.Context, _state, cancellationToken);
                await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Confused);
            }

            _state = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());
            var name1 = _state.ConfuseCounter;

            if (name1 >= 3)
            {
                _state = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());
                var name = _state.ConfuseCounter = 0;
                await _onboardingState.SetAsync(dc.Context, _state, cancellationToken);
                await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Help);
            }
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Check if there was an action submitted from intro card
            var value = dc.Context.Activity.Value;

            if (value.GetType() == typeof(JObject))
            {
                var submit = JObject.Parse(value.ToString());

                if (value != null && (string)submit["id"] == "SaveStudentProfile")
                {
                    Database db = new Database();

                    var userProfile = await _userProfileAccessor.GetAsync(dc.Context, () => new UserProfile(), cancellationToken);
                    User self = userProfile.self;
                    
                    self.Name = (string)submit["StudentVal"];
                    self.EmailAdress = (string)submit["StudentEmailVal"];
                    self.EmailAdress = self.EmailAdress.ToLower();
                    self.Name = self.Name.ToLower();
                    self.PartitionKey = "University of South Florida";
                    self.Classification = "student";
                    userProfile.self = self;
                    await _userProfileAccessor.SetAsync(dc.Context, userProfile, cancellationToken);

                    db.postNewUser(self);
                }
                if (value != null && (string)submit["id"] == "SaveTutorProfile")
                {
                    Database db = new Database();
                    var userProfile = await _userProfileAccessor.GetAsync(dc.Context, () => new UserProfile(), cancellationToken);
                    User self = userProfile.self;
                    self.Name = (string)submit["TutorVal"];
                    self.EmailAdress = (string)submit["TutorEmailVal"];
                    self.EmailAdress = self.EmailAdress.ToLower();
                    self.Name = self.Name.ToLower();
                    self.PartitionKey = "University of South Florida";
                    self.Classification = "tutor";
                    userProfile.self = self;
                    string courses = (string)submit["TutorCourseSelectVal"];


                    db.setTutorCourses(self, courses);
                    db.postNewUser(self);
                    await _userProfileAccessor.SetAsync(dc.Context, userProfile, cancellationToken);

                }
                if (value != null && (string)submit["id"] == "SaveAvailability")
                {
                    Database db = new Database();
                    var userProfile = await _userProfileAccessor.GetAsync(dc.Context, () => new UserProfile(), cancellationToken);
                    User self = userProfile.self;

                    string sel1 = (string)submit["TutorDayCol1"];
                    string sel2 = (string)submit["TutorDayCol2"];
                    string commaDays = sel1;
                    if (sel2 != null ) commaDays = sel1 + "," + sel2;
                    string[] days = commaDays.Split(new char[] { ',' });

                    int Tstart = Int32.Parse((string)submit["TutorTimeStart"]);
                    int Tend = Int32.Parse((string)submit["TutorTimeEnd"]);
                    List<string> availabilities = new List<string>();

                    foreach (string d in days)
                    {
                        for (int i = Tstart; i <= Tend; i++)
                        {
                            string hr;
                            if (i < 10) hr = "0" + i.ToString();
                            else hr = i.ToString();
                            DateTime temp = db.GetNextWeekday(DateTime.Today.Date, (DayOfWeek)Int32.Parse(d));
                            string avl = "D" + temp.Year.ToString() + "_" + temp.Month.ToString() + "_" + temp.Day.ToString() + "_T" + hr;
                            availabilities.Add(avl);
                        }
                    }

                    db.setTutorAvailability(self, availabilities);
                    await dc.CancelAllDialogsAsync();
                }
            }


            var result = await dc.ContinueDialogAsync();

            //if (result.Status == DialogTurnStatus.Complete)
            //{
            //    await CompleteAsync(dc);
            //}
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // The active dialog's stack ended with a complete status
            Database db = new Database();
            var userProfile = await _userProfileAccessor.GetAsync(dc.Context, () => new UserProfile(), cancellationToken);
            User self = userProfile.self;

            await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Completed, self);

            // Request feedback on the last activity.
            //await FeedbackMiddleware.RequestFeedbackAsync(dc.Context, Id);
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            if (dc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrWhiteSpace(dc.Context.Activity.Text))
            {
                // get current activity locale
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var cognitiveModels = _services.CognitiveModelSets[locale];

                // check luis intent
                cognitiveModels.LuisServices.TryGetValue("General", out var luisService);
                if (luisService == null)
                {
                    throw new Exception("The General LUIS Model could not be found in your Bot Services configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<GeneralLuis>(dc.Context, cancellationToken);
                    var intent = luisResult.TopIntent().intent;

                    if (luisResult.TopIntent().score > 0.5)
                    {
                        switch (intent)
                        {
                            case GeneralLuis.Intent.Cancel:
                                {
                                    return await OnCancel(dc);
                                }
                        }
                    }
                }
            }

            return InterruptionAction.NoAction;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            if (dc.ActiveDialog != null && dc.ActiveDialog.Id != nameof(CancelDialog))
            {
                // Don't start restart cancel dialog
                await dc.BeginDialogAsync(nameof(MainDialog));

                // Signal that the dialog is waiting on user response
                return InterruptionAction.StartedDialog;
            }

            var view = new CancelResponses();
            await view.ReplyWith(dc.Context, CancelResponses.ResponseIds.NothingToCancelMessage);

            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var view = new MainResponses();
            await view.ReplyWith(dc.Context, MainResponses.ResponseIds.Help);

            // Signal the conversation was interrupted and should immediately continue
            return InterruptionAction.MessageSentToUser;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
        {
            IUserTokenProvider tokenProvider;
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (!supported)
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
            else
            {
                tokenProvider = (IUserTokenProvider)dc.Context.Adapter;
            }

            await dc.CancelAllDialogsAsync();

            // Sign out user
            var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
            foreach (var token in tokens)
            {
                await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName);
            }

            await dc.Context.SendActivityAsync(MainStrings.LOGOUT);

            return InterruptionAction.StartedDialog;
        }

        private class Events
        {
            public const string TimezoneEvent = "VA.Timezone";
            public const string LocationEvent = "VA.Location";
            public const string SubmitButton = "Action.Submit";
        }
    }
}