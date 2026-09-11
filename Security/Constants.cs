/*
 * Kirin-Tool Read-Only Reference License
 *
 * Licensor:
 * Kethily Daniel & NDX
 *
 * Licensed Work:
 * All files and contents of the Kirin-Tool repository, including source code, documentation, assets, configuration files, build scripts, and associated materials.
 *
 * Contact Information:
 * https://kirintool.cfd
 *
 *
 * Copyright (c) 2026 Kethily Daniel & NDX. All rights reserved.
 *
 * This source code and all associated resources (collectively, the "Work") are
 * published strictly for human review and reference purposes.
 *
 * 1. READ-ONLY ACCESS
 *    You are permitted only to view and read the Work as published in this
 *    repository. No license is granted to download, clone, fork, mirror, cache,
 *    or store the Work for any purpose beyond transient viewing, except where
 *    strictly necessary for standard, incidental operation of the hosting
 *    platform (e.g., your browser's normal rendering of the page).
 *
 * 2. PROHIBITED USES
 *    You are strictly prohibited from:
 *    - Copying, reproducing, or duplicating any part of the Work.
 *    - Modifying, altering, or creating derivative works based on the Work.
 *    - Distributing, publishing, sublicensing, or selling the Work or any
 *      portion or derivative of it.
 *    - Using the Work in any commercial or non-commercial product or project.
 *    - Using any automated means (scripts, scrapers, crawlers, or bots) to
 *      access, index, or extract the Work.
 *
 * 3. ARTIFICIAL INTELLIGENCE / MACHINE LEARNING RESTRICTION
 *    You may not use the Work, in whole or in part, to train, fine-tune,
 *    evaluate, prompt, augment (e.g., retrieval-augmented generation), or
 *    otherwise develop any artificial intelligence or machine learning model,
 *    including large language models ("LLMs"), whether by direct ingestion,
 *    automated scraping, dataset inclusion, or any other method. This includes,
 *    without limitation:
 *    - Submitting the Work as input/context to an AI system or LLM.
 *    - Including the Work in any training, fine-tuning, or evaluation corpus.
 *    - Using the Work to generate embeddings, summaries, or derived training
 *      signals of any kind.
 *
 *    TEXT AND DATA MINING RESERVATION: To the extent any applicable law
 *    (including, without limitation, Article 4 of Directive (EU) 2019/790)
 *    provides an exception or limitation permitting text and data mining absent
 *    an express reservation, the rights holder hereby expressly reserves all
 *    such rights and opts out of any such exception with respect to the Work.
 *
 * 4. NO IMPLIED LICENSE
 *    Nothing in this license shall be construed as granting any license or
 *    right, by implication, estoppel, or otherwise, to any intellectual
 *    property rights in the Work beyond the limited viewing right expressly
 *    stated in Section 1.
 *
 * 5. ENFORCEMENT
 *    Any use of the Work in violation of this license immediately terminates
 *    any permission granted herein and may subject the violator to legal
 *    action for copyright infringement and any other applicable claims.
 *
 * THE WORK IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE, AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHOR
 * OR COPYRIGHT HOLDER BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER LIABILITY
 * ARISING FROM THE WORK OR THE USE OR OTHER DEALINGS IN THE WORK.
 */

namespace Kirin_Tool.Security
{
    public static class Constants
    {
        public const string FBK = @"VkVaTmQyUkZlRlJOVlU1VFZsZFNTMVpIYkVOVlZsWnlZa1prVWxac1NrZFRWVll3VW14a1ZFMUlVazFWZWtJd1VUSnplRk5zVGxaV2FrcGhUVWQ0UkZWV1ZsTlJiRkp5VTIwMWFFMHdXblpaVkVwelUwVTVXVmt6WkZKaE1GcFRWV3hXWVZGc1JsZFVhMUpTWVROU2RWcEVTbXRpYkZWNVpFVktZVTFHV2tOVlZtTTFVMnhHY2xKc1NsSk5WMUp6VlZSR2IxUlhVbFppTTNCaFUwWndTbFJxU2taVE1VcDBXa1ZvVGxaclJqVlhiVEZQVkZkS1ZsUlVUazFsV0ZKRFZqRmtWMDB4V2xkU1dGcFNZVEZLU1ZsV1VtdGxiRkpZVTJ0d1dtSlhkekpXYlhCRFRVWnJlbE5zVW1GaE1WbzFWVlpWTUdWV1VuRlZibWhvVFZVMVNsVlVSbk5VYkZWM1QwUkthMVl4U25WVWJHaFRZMjFTTTJOR1VsZFdSMDU1VlRJd2VHVnNjRWhQV0VKVlpXeGFTRlpzVmpCaGF6VldWV3RrVmxJd05URlhhazVYVmtaYWNWZHJkRlJXYXpVMVUzcEtORk5GTlVaYVJWcFNZa1ZhVVZWV1ZUUk5iRkp5VW10b1RHVnRkekZYYTFwaFZERnNjbVJJVGxwTlNGRXhWVzB4YjA1c1JsaFZhbHBFWWxVeE5GWkdWbnBsUjA1SlYyeGtUMDFGTUhkWlZWSnpZekZLU0UxVVNtcGliRXBVVlhwS2QxbFZOVVZSV0dSb1VtMXplRmRyVmpSVVIwNVpXa2Q0VlZOSVFrVlhiR2hoVXpGR1YxWlVUbFpTTTJod1dXMHdOV0ZHVFhsUFJGWllZbXhLYzFwSE1XdGpiVkpHVTJwYVZVMVhUa3hXTUdSaFVteGFSbU16Y0dsV1JuQXhWMVJLUjJOWFVsaFVhMnhoVmpBMU1GVnNXa3BOVmtaeVVsUktVMVl4Y0V4VVZXaDNZMGROZUZKc1VsSlhSM2hJVld0VmVGWlhVbGxSV0ZwVlltdFdNbGxyWXpWaWJVcEhWMjVTYW1KVWEzbFZiVEZUVVRGV2NWWnVaRlJTVm5CWVdUSmtkMk5zU2xaa1JVNXNWakJhV0ZWNlFUUmxiRnBKWVVkNFZFMUdjRVZXVlZaaFVURnJlVnBHUW1oV2VteEpWa1JCTUU1R1drZFVhbEpYWVRBMWVGbHRlSGRVUjA1MFVtNW9UMVl4UlRGVk1HaGhZVEZ3UjFSdVFrOVNWRVpUVlRCYWMxVkdWbFZUYXpWVVZsZG9jMVpHVlRGa1ZVNXhVbXhTYW1KdGVIWlVWM00xVGtaR1dGcEZOVkpoTUZwRFZXeFdUMkpzYjNkV2EwcFNWa2Q0TUZwRlZURmlSMDVIVkcxMFVGWlVSbFpVTVU0d1lVZE9WMUp1V2xoV1ZGSTBWbXhPTUZOR2IzZFVia3BWVWxWd2RGWnRlRzVOYlZKSVlVWndXRlp0ZDNwVVJFNTNVVEZGZUZrd2RFNU5NbEpXVjFSR1MyUnNaSFZhZWtKcVRWVmFORmt3YUhOUmJGRjVXak5rVDFacmNHOVVNVnBUWkd4S1dWTnNTbWxXVjFKaFUzcE9WMlZXVm5Ka1NFWlVZVzE0VVZSc1VrWmtiSEJHWVVVeFZWTkhhRVpaVmxwTFpHczFXVmRyTld0V1JVcEhWakp6ZUdNeFNrZFVhemxTV2pOQ1MxUlhlRTlWUlRsR1ZGUk9WRkpZUWt4WlZscHZZbTFHU0U5WVpHdE5iRXBWVlZkNFlXRnRUblJUYTA1cVVtMU9NbFF3VG5wbFYxWklUbFJTVG1KcmNGbFdiWFF3WkVkR1ZsVnJiRmRSTTFKSlZWUkNUMVJWTUhoWGJURk1aVzFTVFZSV1VrZFJNSE42VTFSU1YxWllRbk5STW5ONFZVWnNjMkZIYUZwV1ZuQnhWVzAxYjA1R1ZsWmxTR1JvVFVWYWNGcFhlSGRUUmxKWVpFaGtVazF0YUVkWmJGWnJZa1UxY2s5VlNrOU5SM042VmpCT01GWnRVWGRpU0ZKU1ZsWndWMWt3YUhkU1JsWldaRVprVkZkR1duWmFWVlY0WVd4RmVGb3pjR0ZUUmtwVFZWUkNibE13TlhKT1ZXeHFZbFp3WVZscVRtdE9iVXBWWVVoc2FsSkZTbkJhVlZaWFZrVTVWVm96U2s1WFIzTjVWMnBPUjFOck5IZGlSV3hvVm5wUk1GUnJWbFpsYlZaMFkwUmFURTFyV2xoVWJGSktUVWRHUms1WVZsQlhSbFl6Vkd4U1lWTkZNSGRXV0ZwcFlrVktlVmw2UWt0a1JteHVZMFZLYTAxWGFGSlVWRVpoWVd4V1ZWVlliRTFOYlZKRVdUSndTMlJzV25Sa1J6RnBZa1ZLV1ZZd1ZUVlNNVzkzVFZaV1YxSlZjSGRWTUZwclpGWlplVlZ0ZUZWaE0wSkZWRlphUjFSR1JuUmFSa3BUVW14S2RWZHFTbE5oTVZaeVVsaG9ZVlpyV2twWlZXUlRWbFZ6ZDFOcVdrUmlXRUV3VmpCamVGWnNaRWRXVkU1T1VrWktUMVpYZEhOVVZsWjBUbFYwVEUxSVozbFdiRnB6VTBaYWRHRkZXbXhpUmxwNFYydGFiMk5YVmxsWFZGSmFZbFZ3UTFkdE5VTmlNRFZXVDFod1YxWlVRWGRaTUZKUFlqRldSMUZVU21sUmVtZzJXa1JPUm1Wc1RrUlBWemxVVFRBeFRGVnJaRWROYXpWSlUydDBVRkpZUWpKWGFrcDZaV3hTV0ZWdVVteFNNMDE1VjIweFZrNVdSWGRsU0U1cFlsZG9NRlpGV2tkVFJsWklWbXM1YUUweFNsbFVha1pPVGtkV1IyRkhjR0ZOVlVwSFZsUkdTbVZGTlVabFJXaFBZbXhLVGxScVFsZFJNbFpaVlc1c1dGSjZRalZaTVVaM1YxWmtkRk5VVGxoaE1taFRWa1ZhU2sxVk5YRlhWRkpwVFVSc2VGcEdhRWRUYkVaMVkwaHdVMUl3V1RCYVJFSXdVVEZ2ZUZKclVrNWhNbWN3V2xkNFRrNVZNSHBYV0hCcFlrWndTMWxxUWtka1JsRjZWMnRvYkZKWGVETldWbFpMVkRKSmVWVlVRbUZOVjNoSVZUSjBhMkZWVG5KaFJteFhVa1pGZDFZeWRGZE5SMGw1VjJzeFZsSXlVbTlWYlRBMFpWZFdSbHBHY0d4aVYyaERXa1JHYzFFeFVsVmFSRkpRVWxob1VGUlhNVk5pVlRsSVpFaFdhbUZ0VVRGWGExVjRWbGROZVdKRmRHdFdWbXQ2VmpGU1NrMUhVblJpUnpWT1lUQmFjVlp0ZUc5VU1rWllZVEIwVmsweFNsUlVSRUp2VmtVeFNHTkZkRnBXZWxaNFYycEJlRTVIU2tWVmEzQlBaVmhPZVZSdE1XcGtNbEp5VGxWNFRrMXJXbnBaYlhoM1pGZFdSVk5zYUU5Tk1Vb3hWbFJPZGsxV1VuRlJiazVVVFVSc1JsWkhOWE5OVmxJMlducE9WMVo2Um5SWGExSlRWakZXVm1SR1VscGFNMEpYVTNwS1NtUnNWa2xYV0dSWFltMW5lbFY2UWt0aWJGWldWRlJTYW1KVVJrMVpWekZQVlZaT1JXRkZTbXBOYkVweVYydG9UMVV5UlhoV2JsSlRVbTFOTVZsVldrSmtiRTE2Vld0V2FGSXphRFJhUlZweVpWZFNTVmt6Y0ZoV01tTjVWakJqTkdRd05WaE5WRnBQWVRGS2RGRXlNVk5XTVhCMFZtcEthbFpXU2xOV2JGSlRVMnhPYzFkdVdteFdiRnBHVmtSS2QySlhUWGxWYkdocVZrVktUVlZyVWt0bFJrcHlWbGhhVDJGck5VeFhiR1J2VXpCM2VWTnVVbEpoTVhCMVdWWlNhMkpzVGxaV2JUbFRVbXRLY2xWcmFFOU5SbG8yV2tSV2ExSkZOVkZXUmxKYVV6RndWbUZHVmxCV1ZGVjNXVzEwVG1ReFNsaFNhM0JVVFdwR1RWUXdXa3RaVm1SeVlVWm9WVkp1UWxoWFZscHlaRzFXVjFwRmFHcFdSMmh4VlRGV1YySnNSbGxSV0dSU1pXMW5lbFZ0ZEc5Uk1WSnlWbTVvV0ZKNlZsSlpiWEJoVlVkS1NHSkdTbFpXV0ZKRVYycENZVkV4VG01alJXaFdVbFpLY0ZwWE5XOWlSbFY1WWpOYVZsWkdXbGRXYm5CaFZURlNjMkpJVG1GaWJYaFJWRzEwWVUxVk1VZGhla3BYVjBkb1MxWXlNV3BrTVdSMVdrWmFZVTB3Y0ZsVVYzTTBUVzFLYzFSVVZtdFdlbXcxVkZSQ2NrMUdiSE5VYkZKWFRWVndkVlpYZUdGaU1WSllXa1JPUkdKVVVYbFpiR1EwWWxaa1NGZHROVlZOYldReVZGWm9SMU5WTVRaVGExcFNWMFZ3VjFaSE1XOU9iR1J3VDFST1RsWjZhRFJVTUdoU1RUSldSVlJZU21GV1ZWcDJWMWR3YTJFeFRuSlRibVJhVmxaS1MxVnRjM2ROYkZWNlZtNU9hR0V4Y0haWlZsWmhZVlUxVmxwRlpGTlNSbXhNVjJ0b2IxZFhSa2hhZWs1UVVsaFNiMVpXYUZaT1IxSkpWMnhTVmsxcmNEUlViR1JoVlVVeFZWTnNWbWxOVmxwTldXMXpkMDF0U2xkVFZFNVRWbXR3Y0Zrd1dsZFJNa1p5VW01YVUwMUZjRU5WTVZaelV6RnJlVkpxVmxoaWJFWTBWVEJWZDA1WFJrWmhSekZoVFZVd2VsUlhaSFpsYlZaSVUycE9VMUpXU2xwWFZtUnZWRzFGZVZkdFJtRlNlbFp3V2xab2QySXhTa2RSYm5CUFZsVnZlVlJVUmxOT2EzZDVWRzE0YUdGcldsTmFSbVJYWWxaYVdHUklaRXhOTURSNlZERm9WMUpYVFhoaVNHaGFUVzFTV1ZwR1dtOWhiVVY2Vlc1c2FGZEdXa1ZhUmxwclZUQk9kRlJVVm1oaE1VbDVWbFpvZDJRd01YQlBXRVpPVmxoQ1NGUnROVWRrYlU1SVQxWk9URTFYVWtOYVJFWktUV3hTV0ZOWWFHaE5iV2hZV2xWb1MwMUZOVWxqU0U1U1lXeEtNVmxXWkROTmJHOTRVVzF3VGxKNlZqVlpWekExVmpBMU5VOVlVbE5XYTBZMVZXdFdjMDFzY0ZsaU1IUlVWbFpLY0ZkWWNFSk5Sa1pYVTJwQ1YySnVRa2xXUldoclRWZE9OV1JGT1ZWWFJVcDVWR3hTYmsxclRuQk5TRkpOVlhwQ01GVnNWVEZTVld4SFVXeE9WRlpzY0VOV2ExWldXakZOZDFac2NFMVZla0l3VkVaTmQxQlJQVDA9";
    }
}