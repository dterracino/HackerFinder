﻿using HackerFinder.Exceptions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

using HackerFinder.Extensions;

namespace HackerFinder
{
    public class ProfileSearcher
    {
        private readonly IGithubInquisitor _inquisitor;

        public ProfileSearcher(IGithubInquisitor inquisitor)
        {
            if (inquisitor == null)
                throw new ArgumentNullException(nameof(inquisitor));
            _inquisitor = inquisitor;
        }

        public IList<Profile> GetProfilesForLocation(string locationText)
        {
            if(locationText == null)
                throw new ArgumentNullException(nameof(locationText));

            try
            {
                return FindAllProfilesForLocation(locationText).ToList();
            }
            catch (Exception ex)
            {
                throw new GithubQueryingException("A problem occurred searching Github.", ex);
            }            
        }

        public IList<Repository> GetReposForUser(string githubUserId)
        {
            if (string.IsNullOrEmpty(githubUserId))
                throw new ArgumentException(nameof(githubUserId));

            var jsonFromInquisitor = _inquisitor.GetRepoSearchResults(githubUserId);

            try
            {
                var arrayOfTokens = JArray.Parse(jsonFromInquisitor);
                return arrayOfTokens.Select(jt => MakeRepositoryFromToken(jt)).ToList();
            }
            catch
            {
                return Enumerable.Empty<Repository>().ToList();
            }

        }

        private IEnumerable<Profile> FindAllProfilesForLocation(string locationText)
        {
            var contentToString = _inquisitor.GetLocationSearchResults(locationText);

            var json = JObject.Parse(contentToString);
            var array = JArray.Parse(json["items"].ToString());

            for (var index = 0; index < array.Count; index++)
            {
                var profileUrl = (string)array[index]["url"];
                var profileRawResult = _inquisitor.GetVerbatimSearchResults(profileUrl);
                var profileJson = JObject.Parse(profileRawResult);
                var profile = MakeProfileFromJson(profileJson);

                yield return profile;
            }
        }
        private static Profile MakeProfileFromJson(JObject profileJson)
        {
            var nameTokens = ((string)profileJson["name"]).Split(' ');
            var profile = new Profile()
            {
                FirstName = nameTokens[0],
                LastName = nameTokens.Count() > 1 ? nameTokens[1] : string.Empty,
                EmailAddress = (string)profileJson["email"],
                ProfileUrl = (string)profileJson["html_url"]
        };
            return profile;
        }

        private static Repository MakeRepositoryFromToken(JToken token)
        {
            return new Repository()
            {
                Name = token.KeyToString("name"),
                Url = token.KeyToString("html_url"),
                Language = token.KeyToString("language")
            };
        }
    }
}
