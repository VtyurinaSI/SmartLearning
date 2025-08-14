using FluentAssertions;
using OrchestrPatterns.Domain;

namespace OrchestratorPatternsTests
{
    public class SimpleFsmTest
    {
        
        [Fact]
        public void InitialStatus_ShouldBeCreated()
        {
            var checking = new Checking(null);
            Assert.Equal(CheckingStatus.Created, checking.Status);
        }

        [Fact]
        public void Created_To_Compiling()
        {
            var checking = new Checking(null);
            var endStatus = checking.TryFireChecking(CheckingTrigger.StartCompile);
            endStatus.Should().BeTrue();
            Assert.Equal(CheckingStatus.Compiling, checking.Status);
        }

        [Fact]
        public void Compiling_To_Compiled()
        {
            var checking = new Checking(null);
            checking.TryFireChecking(CheckingTrigger.StartCompile);
            checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            Assert.Equal(CheckingStatus.Compiled, checking.Status);
        }

        [Fact]
        public void Compiled_To_Testing()
        {
            var checking = new Checking(null);
            checking.TryFireChecking(CheckingTrigger.StartCompile);
            checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            checking.TryFireChecking(CheckingTrigger.StartTests);
            Assert.Equal(CheckingStatus.Testing, checking.Status);
        }

        [Fact]
        public void Testing_To_Tested()
        {
            var checking = new Checking(null);
            checking.TryFireChecking(CheckingTrigger.StartCompile);
            checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            checking.TryFireChecking(CheckingTrigger.StartTests);
            checking.TryFireChecking(CheckingTrigger.TestsFinished);
            Assert.Equal(CheckingStatus.Tested, checking.Status);
        }

        [Fact]
        public void Tested_To_Reviewing()
        {
            var checking = new Checking(null);
            checking.TryFireChecking(CheckingTrigger.StartCompile);
            checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            checking.TryFireChecking(CheckingTrigger.StartTests);
            checking.TryFireChecking(CheckingTrigger.TestsFinished);
            checking.TryFireChecking(CheckingTrigger.StartReview);
            Assert.Equal(CheckingStatus.Reviewing, checking.Status);
        }

        [Fact]
        public void Reviewing_To_Reviewed()
        {
            var checking = new Checking(null);
            checking.TryFireChecking(CheckingTrigger.StartCompile);
            checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            checking.TryFireChecking(CheckingTrigger.StartTests);
            checking.TryFireChecking(CheckingTrigger.TestsFinished);
            checking.TryFireChecking(CheckingTrigger.StartReview);
            checking.TryFireChecking(CheckingTrigger.ReviewFinished);
            Assert.Equal(CheckingStatus.Reviewed, checking.Status);
        }

        [Fact]
        public void Reviewed_To_Passed()
        {
            var checking = new Checking(null);
            checking.TryFireChecking(CheckingTrigger.StartCompile);
            checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            checking.TryFireChecking(CheckingTrigger.StartTests);
            checking.TryFireChecking(CheckingTrigger.TestsFinished);
            checking.TryFireChecking(CheckingTrigger.StartReview);
            checking.TryFireChecking(CheckingTrigger.ReviewFinished);
            checking.TryFireChecking(CheckingTrigger.Finalize);
            Assert.Equal(CheckingStatus.Passed, checking.Status);
        }

        #region тесты на исключения

        [Fact]
        public void TryFireChecking_InvalidTriggerFromCreated_ThrowsException()
        {
            var checking = new Checking(null);
            var endStatus=
                checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            endStatus.Should().BeFalse();
        }

        [Fact]
        public void TryFireChecking_InvalidTriggerFromCompiling_ThrowsException()
        {
            var checking = new Checking(null);
            checking.TryFireChecking(CheckingTrigger.StartCompile);
            var endStatus = checking.TryFireChecking(CheckingTrigger.TestsFinished);
            endStatus.Should().BeFalse();
        }

        [Fact]
        public void TryFireChecking_InvalidTriggerFromCompiled_ThrowsException()
        {
            var checking = new Checking(null);
            checking.TryFireChecking(CheckingTrigger.StartCompile);
            checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            var endStatus = checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            endStatus.Should().BeFalse();
        }

        [Fact]
        public void TryFireChecking_InvalidTriggerFromTesting_ThrowsException()
        {
            var checking = new Checking(null);
            checking.TryFireChecking(CheckingTrigger.StartCompile);
            checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            checking.TryFireChecking(CheckingTrigger.StartTests);
            var endStatus = checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            endStatus.Should().BeFalse();
        }

        [Fact]
        public void TryFireChecking_InvalidTriggerFromReviewed_ThrowsException()
        {
            var checking = new Checking(null);
            checking.TryFireChecking(CheckingTrigger.StartCompile);
            checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            checking.TryFireChecking(CheckingTrigger.StartTests);
            checking.TryFireChecking(CheckingTrigger.TestsFinished);
            checking.TryFireChecking(CheckingTrigger.StartReview);
            checking.TryFireChecking(CheckingTrigger.ReviewFinished);
            var endStatus = checking.TryFireChecking(CheckingTrigger.CodeCompiled);
            endStatus.Should().BeFalse();
        }
        #endregion
    }
}