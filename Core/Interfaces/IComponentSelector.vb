Imports System.Collections.Generic

Namespace Core.Interfaces

    ''' <summary>
    ''' Contract for the component selection engine.
    ''' Takes a fully populated MissionSpecs object and returns a SelectionResult
    ''' containing the best-fit component for each category, plus any warnings
    ''' generated during selection.
    '''
    ''' Keeping this as an interface means the UI layer never depends on a concrete
    ''' implementation — swap algorithms, mock for testing, or move to a web service
    ''' later without touching MainForm.
    ''' </summary>
    Public Interface IComponentSelector

        ''' <summary>
        ''' Run the selection algorithm against the component database.
        ''' </summary>
        ''' <param name="specs">Mission parameters collected from the UI.</param>
        ''' <returns>
        ''' A <see cref="SelectionResult"/> containing one recommended component per
        ''' category (Nothing if no suitable match exists) and a list of human-readable
        ''' warnings or notes produced during selection.
        ''' </returns>
        Function SelectComponents(specs As Core.Models.MissionSpecs) As Core.Services.SelectionResult

    End Interface

End Namespace
