using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Habilitations;

namespace N4Sentinel.Domain.Tests;

/// <summary>
/// §2.3.2 — les huit profils d'accès et la séparation des droits. Ces règles vivent dans le
/// domaine pour être vérifiables sans base, sans requête HTTP et sans utilisateur connecté.
/// </summary>
public class HabilitationsTests
{
    [Fact]
    public void Les_huit_profils_du_cahier_des_charges_sont_definis()
    {
        Assert.Equal(8, DroitsParProfil.TousLesProfils.Count);
    }

    [Fact]
    public void Chaque_profil_porte_au_moins_le_droit_de_consulter()
    {
        foreach (var profil in DroitsParProfil.TousLesProfils)
        {
            Assert.Contains(Droit.Consulter, DroitsParProfil.Pour(profil));
        }
    }

    [Fact]
    public void L_auditeur_ne_dispose_d_aucun_droit_d_action()
    {
        var droits = DroitsParProfil.Pour(ProfilUtilisateur.Auditeur);

        // « Accéder en lecture aux historiques, preuves, rapports… » — un auditeur qui pourrait
        // agir ne serait plus un auditeur.
        Assert.DoesNotContain(droits, Droits.EstUneAction);
    }

    [Fact]
    public void Le_tos_manager_n_a_ni_pilotage_ni_administration()
    {
        var droits = DroitsParProfil.Pour(ProfilUtilisateur.TosManagerConsultation);

        Assert.DoesNotContain(Droit.ExecuterUneOperationAutorisee, droits);
        Assert.DoesNotContain(Droit.ExecuterUneOperationSensible, droits);
        Assert.DoesNotContain(Droit.GererLeReferentiel, droits);
        Assert.DoesNotContain(Droit.ConfigurerLesConnecteurs, droits);
    }

    [Fact]
    public void L_import_de_logs_n_est_pas_accorde_d_office_au_lecteur()
    {
        // « Importer des logs uniquement si cette autorisation lui est attribuée. »
        Assert.DoesNotContain(
            Droit.ImporterDesLogs,
            DroitsParProfil.Pour(ProfilUtilisateur.LecteurSupportN1));
    }

    [Theory]
    [InlineData(EnvironmentType.Uat)]
    [InlineData(EnvironmentType.Formation)]
    [InlineData(EnvironmentType.Integration)]
    public void Hors_production_le_profil_global_accorde_ses_droits_d_action(EnvironmentType type)
    {
        var droits = ResolveurDHabilitations.Resoudre(
            [ProfilUtilisateur.OperateurN4SupportN2],
            [],
            type);

        Assert.Contains(Droit.ExecuterUneOperationAutorisee, droits);
    }

    [Fact]
    public void En_production_le_profil_global_n_accorde_que_la_consultation()
    {
        var droits = ResolveurDHabilitations.Resoudre(
            [ProfilUtilisateur.AdministrateurN4],
            [],
            EnvironmentType.Production);

        Assert.Contains(Droit.Consulter, droits);
        Assert.DoesNotContain(Droit.ExecuterUneOperationSensible, droits);
        Assert.DoesNotContain(droits, Droits.EstUneAction);
    }

    [Fact]
    public void En_production_une_habilitation_explicite_rend_l_action_possible()
    {
        var droits = ResolveurDHabilitations.Resoudre(
            [ProfilUtilisateur.LecteurSupportN1],
            [ProfilUtilisateur.AdministrateurN4],
            EnvironmentType.Production);

        Assert.Contains(Droit.ExecuterUneOperationSensible, droits);
    }

    [Fact]
    public void Un_utilisateur_sans_aucun_profil_n_a_aucun_droit()
    {
        Assert.Empty(ResolveurDHabilitations.Resoudre([], [], EnvironmentType.Uat));
    }
}

/// <summary>§2.3.2 — règles de séparation des responsabilités.</summary>
public class SeparationDesResponsabilitesTests
{
    private static readonly IReadOnlySet<Droit> DroitsDUnValidateur =
        DroitsParProfil.Pour(ProfilUtilisateur.ValidateurResponsableHabilite);

    [Fact]
    public void Le_demandeur_ne_peut_pas_approuver_sa_propre_operation_en_production()
    {
        var verdict = SeparationDesResponsabilites.PeutApprouverUneOperation(
            demandeurId: "u-1",
            approbateurId: "u-1",
            DroitsDUnValidateur,
            EnvironmentType.Production,
            doubleValidationRequise: false);

        Assert.False(verdict.Autorise);
    }

    [Fact]
    public void Deux_personnes_distinctes_peuvent_lancer_et_approuver()
    {
        var verdict = SeparationDesResponsabilites.PeutApprouverUneOperation(
            demandeurId: "u-1",
            approbateurId: "u-2",
            DroitsDUnValidateur,
            EnvironmentType.Production,
            doubleValidationRequise: true);

        Assert.True(verdict.Autorise);
    }

    [Fact]
    public void La_double_validation_exige_la_separation_meme_hors_production()
    {
        var verdict = SeparationDesResponsabilites.PeutApprouverUneOperation(
            demandeurId: "u-1",
            approbateurId: "u-1",
            DroitsDUnValidateur,
            EnvironmentType.Uat,
            doubleValidationRequise: true);

        Assert.False(verdict.Autorise);
    }

    [Fact]
    public void Sans_droit_d_approbation_l_approbation_est_refusee()
    {
        var verdict = SeparationDesResponsabilites.PeutApprouverUneOperation(
            demandeurId: "u-1",
            approbateurId: "u-2",
            DroitsParProfil.Pour(ProfilUtilisateur.OperateurN4SupportN2),
            EnvironmentType.Uat,
            doubleValidationRequise: true);

        Assert.False(verdict.Autorise);
    }

    [Fact]
    public void Un_administrateur_n4_ne_peut_pas_approuver_son_propre_contournement()
    {
        // Le cas est construit au pire : l'acteur cumule le droit d'approuver.
        var droits = new HashSet<Droit>(DroitsParProfil.Pour(ProfilUtilisateur.AdministrateurN4))
        {
            Droit.Approuver
        };

        var verdict = SeparationDesResponsabilites.PeutApprouverUnContournement(
            demandeurId: "admin-n4",
            approbateurId: "admin-n4",
            droits);

        Assert.False(verdict.Autorise);
    }

    [Fact]
    public void Un_contournement_peut_etre_approuve_par_un_tiers_habilite()
    {
        var verdict = SeparationDesResponsabilites.PeutApprouverUnContournement(
            demandeurId: "admin-n4",
            approbateurId: "validateur",
            DroitsDUnValidateur);

        Assert.True(verdict.Autorise);
    }

    [Fact]
    public void Seul_le_droit_de_gestion_des_roles_permet_de_modifier_les_habilitations()
    {
        Assert.True(SeparationDesResponsabilites.PeutModifierLesHabilitations(
            DroitsParProfil.Pour(ProfilUtilisateur.AdministrateurDeLaSolution)).Autorise);

        Assert.False(SeparationDesResponsabilites.PeutModifierLesHabilitations(
            DroitsParProfil.Pour(ProfilUtilisateur.AdministrateurN4)).Autorise);
    }
}
