﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Markdig;
using Markdig.Syntax;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Markdown.Editor.Preview {
    // Based on https://github.com/madskristensen/MarkdownEditor/blob/master/src/Parsing/MarkdownFactory.cs
    public static class MarkdownFactory {
        private const string AttachedExceptionKey = "attached-exception";
        private static readonly ConditionalWeakTable<ITextSnapshot, MarkdownDocument> _cachedDocuments = new ConditionalWeakTable<ITextSnapshot, MarkdownDocument>();
        private static readonly object _syncRoot = new object();

        static MarkdownFactory() {
            Pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UsePragmaLines()
                .Build();
        }

        public static MarkdownPipeline Pipeline { get; }

        public static Exception GetAttachedException(this MarkdownDocument markdownDocument)
            => markdownDocument.GetData(AttachedExceptionKey) as Exception;

        public static MarkdownDocument ParseToMarkdown(this ITextSnapshot snapshot, string file = null) {
            lock (_syncRoot) {
                return _cachedDocuments.GetValue(snapshot, key => {
                    var text = key.GetText();
                    var markdownDocument = ParseToMarkdown(text);
                    Parsed?.Invoke(snapshot, new ParsingEventArgs(markdownDocument, file, snapshot));
                    return markdownDocument;
                });
            }
        }

        public static MarkdownDocument ParseToMarkdown(string text, MarkdownPipeline pipeline = null) {
            // Safe version that will always return a MarkdownDocument even if there is an exception while parsing
            MarkdownDocument doc;
            pipeline = pipeline ?? Pipeline;

            // Try first to parse a document with all exceptions active
            try {
                doc = Markdig.Markdown.Parse(text, pipeline);
            } catch (Exception ex) {
                // If we have an error, remember it
                doc = new MarkdownDocument { Span = new SourceSpan(0, text.Length - 1) };
                // we attach the exception to the document that will be later displayed to the user
                doc.SetData(AttachedExceptionKey, ex);
            }
            return doc;
        }

        public static event EventHandler<ParsingEventArgs> Parsed;
    }
}
