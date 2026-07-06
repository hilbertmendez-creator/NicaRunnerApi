using NicaRunner.Application.Notifications.EmailTemplates;

namespace NicaRunner.Application.Common.Interfaces;

public interface IEmailTemplateRenderer
{
    RenderedEmail RenderRaceResult(RaceResultEmailModel model);
    RenderedEmail RenderPasswordReset(PasswordResetEmailModel model);
    RenderedEmail RenderWelcomeAccount(WelcomeAccountEmailModel model);
}
