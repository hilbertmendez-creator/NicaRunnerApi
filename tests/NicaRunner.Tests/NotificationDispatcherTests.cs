using Moq;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Notifications;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class NotificationDispatcherTests
{
    private static Mock<INotificationSender> SenderFor(NotificationChannel channel)
    {
        var mock = new Mock<INotificationSender>();
        mock.SetupGet(s => s.Channel).Returns(channel);
        return mock;
    }

    // Enrutamiento correcto: pedir canal Email invoca al sender que reporta
    // Channel == Email, NO al de WhatsApp — validación directa del bug latente
    // que motivó introducir el dispatcher (sin él, cualquier consumer que
    // pidiera INotificationSender singular resolvía al ÚLTIMO registrado
    // = StubWhatsApp, ignorando Resend).
    [Fact]
    public async Task Send_CanalEmail_InvocaAlSenderDeEmailNoAlDeWhatsApp()
    {
        var email = SenderFor(NotificationChannel.Email);
        var whatsapp = SenderFor(NotificationChannel.WhatsApp);
        email.Setup(s => s.SendAsync("a@b.com", "hola", "sub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(true, null));

        var dispatcher = new NotificationDispatcher([email.Object, whatsapp.Object]);
        var result = await dispatcher.SendAsync(NotificationChannel.Email, "a@b.com", "hola", "sub");

        Assert.True(result.Success);
        email.Verify(s => s.SendAsync("a@b.com", "hola", "sub", It.IsAny<CancellationToken>()), Times.Once);
        whatsapp.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Simétrico para WhatsApp — confirma que la Dictionary por canal enruta
    // bien en ambos sentidos, no solo al primero registrado.
    [Fact]
    public async Task Send_CanalWhatsApp_InvocaAlSenderDeWhatsAppNoAlDeEmail()
    {
        var email = SenderFor(NotificationChannel.Email);
        var whatsapp = SenderFor(NotificationChannel.WhatsApp);
        whatsapp.Setup(s => s.SendAsync("555", "hola", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationSendResult(true, null));

        var dispatcher = new NotificationDispatcher([email.Object, whatsapp.Object]);
        var result = await dispatcher.SendAsync(NotificationChannel.WhatsApp, "555", "hola");

        Assert.True(result.Success);
        whatsapp.Verify(s => s.SendAsync("555", "hola", null, It.IsAny<CancellationToken>()), Times.Once);
        email.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Sin sender para el canal pedido → devuelve failure con mensaje explícito,
    // NO lanza. Esto es lo que permite que ForgotPasswordAsync no crashee cuando
    // Resend no está configurado en dev — solo se pierde el envío silencioso.
    [Fact]
    public async Task Send_CanalSinSenderRegistrado_DevuelveFailureNoLanza()
    {
        var email = SenderFor(NotificationChannel.Email);
        var dispatcher = new NotificationDispatcher([email.Object]);

        var result = await dispatcher.SendAsync(NotificationChannel.WhatsApp, "555", "hola");

        Assert.False(result.Success);
        Assert.Contains("WhatsApp", result.ErrorMessage);
        email.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Cero senders registrados: cualquier canal falla con mensaje explícito.
    // Sanity check para el escenario de un entorno de test sin configuración
    // de notificaciones.
    [Fact]
    public async Task Send_SinSendersRegistrados_TodoCanalFalla()
    {
        var dispatcher = new NotificationDispatcher([]);

        var result = await dispatcher.SendAsync(NotificationChannel.Email, "a@b.com", "hola");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    // Dos senders reclamando el mismo canal es un bug de config y se detecta
    // en el startup del scope (ArgumentException de ToDictionary), no una
    // vez ya está enviando. Es exactamente el fail-fast que queremos.
    [Fact]
    public void Ctor_DosSendersMismoCanal_LanzaArgumentException()
    {
        var email1 = SenderFor(NotificationChannel.Email);
        var email2 = SenderFor(NotificationChannel.Email);

        Assert.Throws<ArgumentException>(() =>
            new NotificationDispatcher([email1.Object, email2.Object]));
    }
}
