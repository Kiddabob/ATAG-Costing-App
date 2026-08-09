namespace ATAG.Costing.Application.Preferences;

public interface IAppPreferencesService
{
    AppPreferences Load();

    void Save(AppPreferences preferences);
}
