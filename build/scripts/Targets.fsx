#load @"Projects.fsx"
#load @"Paths.fsx"
#load @"Versioning.fsx"
#load @"Commandline.fsx"
#load @"Tooling.fsx"
#load @"Building.fsx"

open Fake
open Fake.Git

open Building
open Versioning
open Commandline

Commandline.parse()

Target "Build" <| fun _ -> traceHeader "STARTING BUILD"

Target "VerifyClean" <| fun _ -> 
    match isCleanWorkingCopy "." with 
    | true -> traceHeader "Current checkout is clean, proceeding with release" 
    | false -> 
        traceError "Current working dir is NOT clean aborting"
        failwithf "Current working dir is NOT clean aborting"

Target "VerifyVersionChange" <| fun _ -> 
    match getBuildParam "versionchanged" with
    | "1" -> Versioning.BumpGlobalVersion Commandline.providedProjects
    | _ ->
        traceError "None of the packages seem to have bumped versions so we can not release at this time"
        failwithf "None of the packages seem to have bumped versions so we can not release at this time"
    
Target "Clean" Build.Clean

Target "Restore" Build.Restore

Target "RewriteBenchmarkDotNetExporter" Build.RewriteBenchmarkDotNetExporter

Target "FullBuild"  <| fun _ -> 
    Build.Compile Commandline.projects

Target "Version" <| fun _ -> 
    let changedResults = 
        Commandline.projects
        |> List.map (fun p -> Versioning.writeVersionIntoVersionsJson (p.Project.project) (p.Informational.ToString()))
        |> List.contains true

    setBuildParam "versionchanged" (if changedResults then "1" else "0")

Target "Pack" <| fun _ -> 
    Build.CreateNugetPackage Commandline.projects
    Versioning.ValidateArtifacts Commandline.projects

Target "Canary" <| fun _ -> traceHeader "Running Canary"

Target "Release" <| fun _ -> traceHeader "Running Release"

// Dependencies
"Clean"
    =?> ("VerifyClean", getBuildParam "target" = "release")
    =?> ("Version", getBuildParam "target" = "release")
    =?> ("VerifyVersionChange", getBuildParam "target" = "release")
    ==> "Restore"
    ==> "FullBuild"
    ==> "Build"

"Build"
  ==> "RewriteBenchmarkDotNetExporter"
  ==> "Pack"

"Pack"
  ==> "Release"

"Pack"
  ==> "Canary"

"Dump"

RunTargetOrListTargets()