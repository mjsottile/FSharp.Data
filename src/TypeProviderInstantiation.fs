﻿namespace ProviderImplementation

open System
open System.IO
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open FSharp.Data.Runtime
open FSharp.Data.Runtime.Freebase.FreebaseRequests

type CsvProviderArgs = 
    { Sample : string
      Separators : string
      InferRows : int
      Schema : string
      HasHeaders : bool
      IgnoreErrors : bool
      AssumeMissingValues : bool
      PreferOptionals : bool
      Quote : char
      MissingValues : string
      CacheRows : bool
      Culture : string
      Encoding : string
      ResolutionFolder : string
      EmbeddedResource : string }

type XmlProviderArgs = 
    { Sample : string
      SampleIsList : bool
      Global : bool
      Culture : string
      Encoding : string
      ResolutionFolder : string
      EmbeddedResource : string 
      InferTypesFromValues : bool }

type JsonProviderArgs = 
    { Sample : string
      SampleIsList : bool
      RootName : string
      Culture : string
      Encoding : string
      ResolutionFolder : string
      EmbeddedResource : string 
      InferTypesFromValues : bool }

type WorldBankProviderArgs =
    { Sources : string
      Asynchronous : bool }

type FreebaseProviderArgs =
    { Key : string
      ServiceUrl : string
      NumIndividuals : int
      UseUnitsOfMeasure : bool 
      Pluralize : bool 
      SnapshotDate : string
      LocalCache : bool
      AllowLocalQueryEvaluation : bool
      UseRefinedTypes: bool }

type TypeProviderInstantiation = 
    | Csv of CsvProviderArgs
    | Xml of XmlProviderArgs
    | Json of JsonProviderArgs
    | WorldBank of WorldBankProviderArgs
    | Freebase of FreebaseProviderArgs

    member x.GenerateType resolutionFolder runtimeAssembly =
        let f, args =
            match x with
            | Csv x -> 
                (fun cfg -> new CsvProvider(cfg) :> TypeProviderForNamespaces),
                [| box x.Sample
                   box x.Separators
                   box x.InferRows
                   box x.Schema
                   box x.HasHeaders
                   box x.IgnoreErrors
                   box x.AssumeMissingValues
                   box x.PreferOptionals
                   box x.Quote
                   box x.MissingValues
                   box x.CacheRows
                   box x.Culture
                   box x.Encoding
                   box x.ResolutionFolder 
                   box x.EmbeddedResource |] 
            | Xml x ->
                (fun cfg -> new XmlProvider(cfg) :> TypeProviderForNamespaces),
                [| box x.Sample
                   box x.SampleIsList
                   box x.Global
                   box x.Culture
                   box x.Encoding
                   box x.ResolutionFolder 
                   box x.EmbeddedResource
                   box x.InferTypesFromValues |] 
            | Json x -> 
                (fun cfg -> new JsonProvider(cfg) :> TypeProviderForNamespaces),
                [| box x.Sample
                   box x.SampleIsList
                   box x.RootName
                   box x.Culture
                   box x.Encoding
                   box x.ResolutionFolder 
                   box x.EmbeddedResource 
                   box x.InferTypesFromValues |] 
            | WorldBank x ->
                (fun cfg -> new WorldBankProvider(cfg) :> TypeProviderForNamespaces),
                [| box x.Sources
                   box x.Asynchronous |] 
            | Freebase x ->
                (fun cfg -> new FreebaseTypeProvider(cfg) :> TypeProviderForNamespaces),
                [| box x.Key
                   box x.ServiceUrl
                   box x.NumIndividuals
                   box x.UseUnitsOfMeasure 
                   box x.Pluralize
                   box x.SnapshotDate
                   box x.LocalCache
                   box x.AllowLocalQueryEvaluation 
                   box x.UseRefinedTypes |]
        Debug.generate resolutionFolder runtimeAssembly f args

    override x.ToString() =
        match x with
        | Csv x -> 
            ["Csv"
             x.Sample
             x.Separators
             x.Schema.Replace(',', ';')
             x.HasHeaders.ToString()
             x.AssumeMissingValues.ToString()
             x.PreferOptionals.ToString()
             x.MissingValues
             x.Culture
             x.Encoding ]
        | Xml x -> 
            ["Xml"
             x.Sample
             x.SampleIsList.ToString()
             x.Global.ToString()
             x.Culture
             x.InferTypesFromValues.ToString() ]
        | Json x -> 
            ["Json"
             x.Sample
             x.SampleIsList.ToString()
             x.RootName
             x.Culture
             x.InferTypesFromValues.ToString() ]
        | WorldBank x -> 
            ["WorldBank"
             x.Sources
             x.Asynchronous.ToString()]
        | Freebase x -> 
            ["Freebase"
             x.NumIndividuals.ToString()
             x.UseUnitsOfMeasure.ToString()
             x.Pluralize.ToString()]
        |> String.concat ","

    member x.ExpectedPath outputFolder = 
        Path.Combine(outputFolder, (x.ToString().Replace(">", "&gt;").Replace("<", "&lt;").Replace("://", "_").Replace("/", "_") + ".expected"))

    member x.Dump resolutionFolder outputFolder runtimeAssembly signatureOnly ignoreOutput =
        let replace (oldValue:string) (newValue:string) (str:string) = str.Replace(oldValue, newValue)        
        let output = 
            x.GenerateType resolutionFolder runtimeAssembly
            |> match x with
               | Freebase _ -> Debug.prettyPrint signatureOnly ignoreOutput 5 10
               | _ -> Debug.prettyPrint signatureOnly ignoreOutput 10 100
            |> replace "FSharp.Data.Runtime." "FDR."
            |> replace resolutionFolder "<RESOLUTION_FOLDER>"
        if outputFolder <> "" then
            File.WriteAllText(x.ExpectedPath outputFolder, output)
        output

    static member Parse (line:string) =
        let args = line.Split [|','|]
        match args.[0] with
        | "Csv" ->
            Csv { Sample = args.[1]
                  Separators = args.[2]
                  InferRows = Int32.MaxValue
                  Schema = args.[3].Replace(';', ',')
                  HasHeaders = args.[4] |> bool.Parse
                  IgnoreErrors = false
                  AssumeMissingValues = args.[5] |> bool.Parse
                  PreferOptionals = args.[6] |> bool.Parse
                  Quote = '"'
                  MissingValues = args.[7]
                  Culture = args.[8]
                  Encoding = args.[9]
                  CacheRows = false
                  ResolutionFolder = ""
                  EmbeddedResource = "" }
        | "Xml" ->
            Xml { Sample = args.[1]
                  SampleIsList = args.[2] |> bool.Parse
                  Global = args.[3] |> bool.Parse
                  Culture = args.[4]
                  Encoding = ""
                  ResolutionFolder = ""
                  EmbeddedResource = "" 
                  InferTypesFromValues = args.[5] |> bool.Parse }
        | "Json" ->
            Json { Sample = args.[1]
                   SampleIsList = args.[2] |> bool.Parse
                   RootName = args.[3]
                   Culture = args.[4] 
                   Encoding = ""
                   ResolutionFolder = ""
                   EmbeddedResource = "" 
                   InferTypesFromValues = args.[5] |> bool.Parse }
        | "WorldBank" ->
            WorldBank { Sources = args.[1]
                        Asynchronous = args.[2] |> bool.Parse }
        | "Freebase" ->
            Freebase { Key = args.[1]
                       NumIndividuals = args.[2] |> Int32.Parse
                       UseUnitsOfMeasure = args.[3] |> bool.Parse
                       Pluralize = args.[4] |> bool.Parse
                       SnapshotDate = ""
                       ServiceUrl = FreebaseQueries.DefaultServiceUrl
                       LocalCache = true
                       AllowLocalQueryEvaluation = true 
                       UseRefinedTypes = true }
        | _ -> failwithf "Unknown: %s" args.[0]

open System.Runtime.CompilerServices

[<assembly:InternalsVisibleToAttribute("FSharp.Data.DesignTime.Tests")>]
do()
