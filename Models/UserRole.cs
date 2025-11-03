namespace HelpFast_Pim.Models;

public enum UserRole
{
    Cliente = 3,
    Tecnico = 2,
    Admin = 1
}

public static class UserRoleExtensions
{
    public static string GetDisplayName(this UserRole role)
    {
        return role switch
        {
            UserRole.Cliente => "Cliente",
            UserRole.Tecnico => "Técnico",
            UserRole.Admin => "Admin",
            _ => "Desconhecido"
        };
    }

    public static string GetDescription(this UserRole role)
    {
        return role switch
        {
            UserRole.Cliente => "Pode abrir e visualizar seus próprios chamados",
            UserRole.Tecnico => "Pode visualizar e resolver todos os chamados",
            UserRole.Admin => "Pode gerenciar usuários e configurações do sistema",
            _ => "Papel não definido"
        };
    }

    public static bool CanRegisterSelf(this UserRole role)
    {
        return role == UserRole.Cliente;
    }

    public static bool CanManageUsers(this UserRole role)
    {
        return role == UserRole.Admin;
    }

    public static bool CanViewAllTickets(this UserRole role)
    {
        return role == UserRole.Tecnico || role == UserRole.Admin;
    }
}