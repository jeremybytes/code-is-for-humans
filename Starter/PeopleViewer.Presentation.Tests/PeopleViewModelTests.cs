using Moq;
using PeopleViewer.Common;

namespace PeopleViewer.Presentation.Tests;

public partial class PeopleViewModelTests
{
    // Setup
    public PeopleViewModelTests()
    {
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
    }

    #region TodaysWinners Initialization

    [Fact]
    public void PeopleViewModel_OnInitialization_TodaysWinnersIsPopulated()
    {
        // Arrange
        var viewModel = GetStandardViewModel();

        // Act
        viewModel.Initialize();

        // Assert
        Assert.NotNull(viewModel.TodaysWinners);
    }

    [Fact]
    public void PeopleViewModel_OnInitializationAndTodaysOrdersMissing_ThrowsException()
    {
        // Arrange
        var dataReader = GetFakeDataReader();
        var viewModel = GetCustomViewModel(dataReader, null);

        // Act / Assert
        try
        {
            viewModel.Initialize();
            Assert.Fail("No exception thrown when TodaysWinners is missing");
        }
        catch (MissingFieldException)
        {
            // Pass
        }
        catch (Exception)
        {
            Assert.Fail("Wrong exception thrown: expecting MissingFieldException");
        }
    }

    #endregion

    #region DataReader Initialization

    [Fact]
    public void PeopleViewModel_OnInitialization_DataReaderIsPopulate()
    {
        var viewModel = GetStandardViewModel();
        viewModel.Initialize();
        Assert.NotNull(viewModel.DataReader);
    }

    [Fact]
    public void PeopleViewModel_OnInitializationAndDataReaderMissing_ThrowsException()
    {
        var winners = GetFakeWinners();
        var viewModel = GetCustomViewModel(null, winners);
        try
        {
            viewModel.Initialize();
            Assert.Fail("No exception thrown when DataReader is missing");
        }
        catch (MissingFieldException)
        {
            // Pass
        }
        catch (Exception)
        {
            Assert.Fail("Wrong exception thrown: expecting MissingFieldException");
        }
    }

    #endregion

    #region DataReader Caching

    [Fact]
    public void DataReader_OnRefreshAndCacheExpired_DataReaderIsCalledTwice()
    {
        var mockReader = GetMockDataReader();
        var winners = GetFakeWinners();
        var viewModel = GetCustomViewModel(mockReader.Object, winners);
        var tracker = new PropertyChangeTracker(viewModel);
        viewModel.Initialize();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);

        viewModel.LastRefreshTime = DateTime.Now.AddHours(-1);
        tracker.Reset();
        viewModel.RefreshPeople();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);

        mockReader.Verify(s => s.GetPeople(), Times.Exactly(2));
    }

    [Fact]
    public void DataReader_OnRefreshAndCacheNotExpired_DataReaderIsCalledOnce()
    {
        var mockReader = GetMockDataReader();
        var winners = GetFakeWinners();
        var viewModel = GetCustomViewModel(mockReader.Object, winners);
        var tracker = new PropertyChangeTracker(viewModel);
        viewModel.Initialize();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);

        tracker.Reset();
        viewModel.RefreshPeople();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);

        mockReader.Verify(s => s.GetPeople(), Times.Once);
    }

    #endregion

    #region DataReaderExceptions

    [Fact]
    public void PeopleViewModel_OnInitializationWithDataReaderException_ThrowsExceptionOnCurrentThread()
    {
        var dataReader = GetExceptionDataReader();
        var winners = GetFakeWinners();
        var viewModel = GetCustomViewModel(dataReader, winners);

        var tracker = new PropertyChangeTracker(viewModel);
        viewModel.Initialize();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);

        Assert.NotNull(viewModel.ViewModelException);
    }

    [Fact]
    public void PeopleViewModel_OnInitializationWithNoDataReaderException_NoExceptionThrown()
    {
        var dataReader = GetFakeDataReader();
        var winners = GetFakeWinners();
        var viewModel = GetCustomViewModel(dataReader, winners);

        var tracker = new PropertyChangeTracker(viewModel);
        viewModel.Initialize();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);

        Assert.Null(viewModel.ViewModelException);
    }

    #endregion

    #region Filters

    [Fact]
    public void People_FilterIncludes70s_70sRecordIsIncluded()
    {
        var viewModel = GetStandardViewModel();
        var tracker = new PropertyChangeTracker(viewModel);
        viewModel.Initialize();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);
        Assert.Contains(test70sPerson, viewModel.People);

        viewModel.Include70s = true;

        Assert.Contains(test70sPerson, viewModel.People);
    }

    [Fact]
    public void People_FilterDoesNotInclude70s_70sRecordIsNotIncluded()
    {
        var viewModel = GetStandardViewModel();
        var tracker = new PropertyChangeTracker(viewModel);
        viewModel.Initialize();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);
        Assert.Contains(test70sPerson, viewModel.People);

        viewModel.Include70s = false;

        Assert.DoesNotContain(test70sPerson, viewModel.People);
    }

    [Fact]
    public void People_FilterIncludes00s_00sRecordIsIncluded()
    {
        var viewModel = GetStandardViewModel();
        var tracker = new PropertyChangeTracker(viewModel);
        viewModel.Initialize();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);
        Assert.Contains(test00sPerson, viewModel.People);

        viewModel.Include00s = true;

        Assert.Contains(test00sPerson, viewModel.People);
    }

    [Fact]
    public void People_FilterDoesNotInclude00s_00sRecordIsNotIncluded()
    {
        var viewModel = GetStandardViewModel();
        var tracker = new PropertyChangeTracker(viewModel);
        viewModel.Initialize();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);
        Assert.Contains(test00sPerson, viewModel.People);

        viewModel.Include00s = false;

        Assert.DoesNotContain(test00sPerson, viewModel.People);
    }

    #endregion

    #region Filter Reset

    [Fact]
    public void Filters_OnRefreshAndCacheExpired_AreResetToDefaults()
    {
        // Arrange
        var mockReader = GetMockDataReader();
        var winners = GetFakeWinners();
        var viewModel = GetCustomViewModel(mockReader.Object, winners);
        var tracker = new PropertyChangeTracker(viewModel);
        viewModel.Include70s = false;
        viewModel.Include80s = false;
        viewModel.Include90s = false;
        viewModel.Include00s = false;
        viewModel.Include10s = false;
        viewModel.Initialize();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);

        // Act
        viewModel.LastRefreshTime = DateTime.Now.AddHours(-1);
        tracker.Reset();
        viewModel.RefreshPeople();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);
        mockReader.Verify(s => s.GetPeople(), Times.Exactly(2),
            "DataReader was not called twice");

        // Assert
        Assert.True(viewModel.Include70s, "Include70s filter was not reset");
        Assert.True(viewModel.Include80s, "Include80s filter was not reset");
        Assert.True(viewModel.Include90s, "Include90s filter was not reset");
        Assert.True(viewModel.Include00s, "Include00s filter was not reset");
        Assert.True(viewModel.Include10s, "Include10s filter was not reset");
    }

    [Fact]
    public void Filters_OnRefreshAndCacheNotExpired_AreResetToDefaults()
    {
        // Arrange
        var mockReader = GetMockDataReader();
        var winners = GetFakeWinners();
        var viewModel = GetCustomViewModel(mockReader.Object, winners);
        var tracker = new PropertyChangeTracker(viewModel);
        viewModel.Include70s = false;
        viewModel.Include80s = false;
        viewModel.Include90s = false;
        viewModel.Include00s = false;
        viewModel.Include10s = false;
        viewModel.Initialize();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);

        // Act
        tracker.Reset();
        viewModel.RefreshPeople();
        tracker.WaitForChange(nameof(viewModel.LastRefreshTime), 1);
        mockReader.Verify(s => s.GetPeople(), Times.Once,
            "DataReader was not called once");

        // Assert
        Assert.True(viewModel.Include70s, "Include70s filter was not reset");
        Assert.True(viewModel.Include80s, "Include80s filter was not reset");
        Assert.True(viewModel.Include90s, "Include90s filter was not reset");
        Assert.True(viewModel.Include00s, "Include00s filter was not reset");
        Assert.True(viewModel.Include10s, "Include10s filter was not reset");
    }

    #endregion

    #region Winners Item Selection

    [Fact]
    public void WinnersSelectedPeople_AddToWinnersWithNewPerson_PersonAdded()
    {
        var viewModel = GetStandardViewModel();
        viewModel.Initialize();
        var newPerson = new Person(100, "Rogers", "Peter",
            new DateTime(2013, 01, 01), 5);
        Assert.DoesNotContain(newPerson, viewModel.TodaysWinners.SelectedPeople);

        viewModel.AddToWinners(newPerson);

        Assert.Contains(newPerson, viewModel.TodaysWinners.SelectedPeople);
    }

    [Fact]
    public void WinnersSelectedPeople_AddToWinnersWithExistingPerson_PersonIsNotAdded()
    {
        var viewModel = GetStandardViewModel();
        viewModel.Initialize();
        var newPerson = new Person(100, "Rogers", "Peter",
            new DateTime(2013, 01, 01), 5);
        viewModel.AddToWinners(newPerson);
        Assert.Contains(newPerson, viewModel.TodaysWinners.SelectedPeople);
        var oldCount = viewModel.TodaysWinners.SelectedPeople.Count;

        viewModel.AddToWinners(newPerson);
        var newCount = viewModel.TodaysWinners.SelectedPeople.Count;

        Assert.Equal(oldCount, newCount);
        Assert.Contains(newPerson, viewModel.TodaysWinners.SelectedPeople);
    }

    [Fact]
    public void WinnersSelectedPeople_RemoveFromWinnersWithExistingPerson_PersonIsRemoved()
    {
        var viewModel = GetStandardViewModel();
        viewModel.Initialize();
        var newPerson = new Person(100, "Rogers", "Peter",
            new DateTime(2013, 01, 01), 5);
        viewModel.AddToWinners(newPerson);
        Assert.Contains(newPerson, viewModel.TodaysWinners.SelectedPeople);
        var oldCount = viewModel.TodaysWinners.SelectedPeople.Count;

        viewModel.RemoveFromWinners(newPerson);
        var newCount = viewModel.TodaysWinners.SelectedPeople.Count;

        Assert.NotEqual(oldCount, newCount);
        Assert.DoesNotContain(newPerson, viewModel.TodaysWinners.SelectedPeople);
    }

    [Fact]
    public void WinnersSelectedPeople_RemoveFromWinnersWithNewPerson_SelectedPeopleIsUnchanged()
    {
        var viewModel = GetStandardViewModel();
        viewModel.Initialize();
        var newPerson = new Person(100, "Rogers", "Peter",
            new DateTime(2013, 01, 01), 5);
        Assert.DoesNotContain(newPerson, viewModel.TodaysWinners.SelectedPeople);
        var oldCount = viewModel.TodaysWinners.SelectedPeople.Count;

        viewModel.RemoveFromWinners(newPerson);
        var newCount = viewModel.TodaysWinners.SelectedPeople.Count;

        Assert.Equal(oldCount, newCount);
        Assert.DoesNotContain(newPerson, viewModel.TodaysWinners.SelectedPeople);
    }

    #endregion
}
