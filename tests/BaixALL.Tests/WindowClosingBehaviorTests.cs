using System.ComponentModel;
using System.Windows;
using Xunit;

namespace BaixALL.Tests;

public class WindowClosingBehaviorTests
{
    private class FakeWindowClosingHandler
    {
        public bool HasActiveDownloads { get; set; }
        public MessageBoxResult UserPromptResult { get; set; } = MessageBoxResult.No;
        public int PromptCallCount { get; private set; }
        public int CancelAllCallCount { get; private set; }

        public MessageBoxResult PromptUser()
        {
            PromptCallCount++;
            return UserPromptResult;
        }

        public void CancelAllDownloads()
        {
            CancelAllCallCount++;
        }
    }

    private static void HandleWindowClosing(FakeWindowClosingHandler handler, CancelEventArgs e)
    {
        if (handler.HasActiveDownloads)
        {
            var result = handler.PromptUser();
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            handler.CancelAllDownloads();
        }
    }

    [Fact]
    public void HandleWindowClosing_WhenActiveDownloadsAndUserChoosesNo_CancelsWindowClosing()
    {
        var handler = new FakeWindowClosingHandler
        {
            HasActiveDownloads = true,
            UserPromptResult = MessageBoxResult.No
        };

        var args = new CancelEventArgs();
        HandleWindowClosing(handler, args);

        Assert.True(args.Cancel, "A janela não deveria fechar quando o usuário escolhe 'Não'");
        Assert.Equal(1, handler.PromptCallCount);
        Assert.Equal(0, handler.CancelAllCallCount);
    }

    [Fact]
    public void HandleWindowClosing_WhenActiveDownloadsAndUserChoosesYes_CancelsDownloadsAndAllowsClose()
    {
        var handler = new FakeWindowClosingHandler
        {
            HasActiveDownloads = true,
            UserPromptResult = MessageBoxResult.Yes
        };

        var args = new CancelEventArgs();
        HandleWindowClosing(handler, args);

        Assert.False(args.Cancel, "A janela deve prosseguir com o fechamento quando o usuário confirma");
        Assert.Equal(1, handler.PromptCallCount);
        Assert.Equal(1, handler.CancelAllCallCount);
    }

    [Fact]
    public void HandleWindowClosing_WhenNoActiveDownloads_AllowsCloseWithoutPrompt()
    {
        var handler = new FakeWindowClosingHandler
        {
            HasActiveDownloads = false
        };

        var args = new CancelEventArgs();
        HandleWindowClosing(handler, args);

        Assert.False(args.Cancel);
        Assert.Equal(0, handler.PromptCallCount);
        Assert.Equal(0, handler.CancelAllCallCount);
    }
}
