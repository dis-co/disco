module Iris.Web.Core.MockData

open System
open System.Collections.Generic
open Fable.Core
open Fable.Import
open Fable.Core.JsInterop
open Fable.PowerPack
open Iris.Core

let loremIpsum =
  [|"Lorem ipsum dolor sit amet"
    "consectetur adipiscing elit"
    "sed do eiusmod tempor incididunt"
    "ut labore et dolore magna aliqua"
    "Ut enim ad minim veniam"
    "quis nostrud exercitation ullamco laboris"
    "nisi ut aliquip ex ea commodo consequat"
    "Duis aute irure dolor in reprehenderit"
    "in voluptate velit esse cillum dolore eu fugiat nulla pariatur"
    "Excepteur sint occaecat cupidatat non proident"
    "sunt in culpa qui officia deserunt mollit anim id est laborum"|]

let text = String.concat "\n" loremIpsum

let image = [| 255uy; 216uy; 255uy; 224uy; 0uy; 16uy; 74uy; 70uy; 73uy; 70uy; 0uy; 1uy; 1uy; 1uy; 0uy; 72uy; 0uy; 72uy; 0uy; 0uy; 255uy; 219uy; 0uy; 67uy; 0uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 255uy; 219uy; 0uy; 67uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 255uy; 194uy; 0uy; 17uy; 8uy; 0uy; 32uy; 0uy; 30uy; 3uy; 1uy; 17uy; 0uy; 2uy; 17uy; 1uy; 3uy; 17uy; 1uy; 255uy; 196uy; 0uy; 26uy; 0uy; 0uy; 1uy; 5uy; 1uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 9uy; 1uy; 4uy; 5uy; 7uy; 8uy; 6uy; 255uy; 196uy; 0uy; 27uy; 1uy; 0uy; 1uy; 5uy; 1uy; 1uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 5uy; 0uy; 2uy; 6uy; 7uy; 8uy; 3uy; 4uy; 255uy; 218uy; 0uy; 12uy; 3uy; 1uy; 0uy; 2uy; 16uy; 3uy; 16uy; 0uy; 0uy; 1uy; 33uy; 65uy; 235uy; 59uy; 148uy; 132uy; 203uy; 153uy; 107uy; 26uy; 249uy; 192uy; 214uy; 113uy; 108uy; 225uy; 134uy; 162uy; 217uy; 178uy; 91uy; 136uy; 210uy; 53uy; 98uy; 238uy; 252uy; 119uy; 192uy; 110uy; 68uy; 23uy; 90uy; 198uy; 39uy; 146uy; 233uy; 13uy; 230uy; 130uy; 235uy; 65uy; 42uy; 180uy; 243uy; 166uy; 214uy; 134uy; 232uy; 160uy; 159uy; 255uy; 196uy; 0uy; 29uy; 16uy; 0uy; 1uy; 5uy; 1uy; 0uy; 3uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 5uy; 1uy; 3uy; 4uy; 6uy; 7uy; 2uy; 19uy; 20uy; 22uy; 255uy; 218uy; 0uy; 8uy; 1uy; 1uy; 0uy; 1uy; 5uy; 2uy; 209uy; 238uy; 18uy; 169uy; 183uy; 84uy; 214uy; 243uy; 191uy; 9uy; 29uy; 170uy; 128uy; 200uy; 204uy; 132uy; 196uy; 179uy; 176uy; 53uy; 55uy; 219uy; 110uy; 197uy; 34uy; 32uy; 171uy; 60uy; 86uy; 222uy; 30uy; 29uy; 50uy; 78uy; 147uy; 209uy; 214uy; 195uy; 40uy; 19uy; 95uy; 93uy; 6uy; 36uy; 98uy; 183uy; 97uy; 200uy; 222uy; 69uy; 84uy; 98uy; 111uy; 27uy; 43uy; 194uy; 185uy; 62uy; 63uy; 58uy; 169uy; 179uy; 215uy; 25uy; 237uy; 77uy; 179uy; 120uy; 212uy; 152uy; 210uy; 198uy; 255uy; 0uy; 255uy; 196uy; 0uy; 40uy; 17uy; 0uy; 1uy; 4uy; 1uy; 3uy; 4uy; 1uy; 4uy; 3uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 3uy; 1uy; 2uy; 4uy; 5uy; 6uy; 7uy; 17uy; 18uy; 8uy; 19uy; 20uy; 33uy; 0uy; 21uy; 34uy; 35uy; 49uy; 50uy; 81uy; 97uy; 255uy; 218uy; 0uy; 8uy; 1uy; 3uy; 1uy; 1uy; 63uy; 1uy; 207uy; 51uy; 75uy; 12uy; 54uy; 223uy; 12uy; 123uy; 32uy; 50uy; 117uy; 13uy; 180uy; 187uy; 72uy; 89uy; 11uy; 188uy; 136uy; 113uy; 229uy; 87uy; 137uy; 172uy; 174uy; 88uy; 86uy; 49uy; 188uy; 185uy; 49uy; 251uy; 233uy; 25uy; 228uy; 144uy; 178uy; 98uy; 143uy; 155uy; 140uy; 5uy; 119uy; 30uy; 5uy; 104uy; 121uy; 71uy; 200uy; 241uy; 217uy; 123uy; 248uy; 151uy; 244uy; 178uy; 209uy; 174uy; 224uy; 171uy; 26uy; 210uy; 9uy; 211uy; 151uy; 173uy; 155uy; 248uy; 206uy; 237uy; 149uy; 81uy; 81uy; 81uy; 23uy; 223uy; 180uy; 245uy; 242uy; 243uy; 36uy; 166uy; 199uy; 162uy; 150uy; 69uy; 156uy; 248uy; 129uy; 123uy; 99uy; 158uy; 64uy; 33uy; 186uy; 92uy; 65uy; 77uy; 159uy; 217uy; 111uy; 46uy; 196uy; 16uy; 201uy; 56uy; 16uy; 230uy; 35uy; 184uy; 137uy; 137uy; 205uy; 163uy; 71uy; 189uy; 170uy; 82uy; 12uy; 124uy; 158uy; 154uy; 115uy; 145uy; 91uy; 229uy; 120uy; 203uy; 47uy; 46uy; 160uy; 134uy; 182uy; 92uy; 187uy; 43uy; 68uy; 20uy; 0uy; 72uy; 143uy; 45uy; 177uy; 32uy; 6uy; 74uy; 142uy; 8uy; 157uy; 38uy; 49uy; 10uy; 35uy; 153uy; 0uy; 141uy; 113uy; 200uy; 142uy; 78uy; 69uy; 115uy; 191uy; 24uy; 219uy; 197uy; 137uy; 213uy; 252uy; 82uy; 78uy; 137uy; 166uy; 240uy; 194uy; 199uy; 20uy; 210uy; 109uy; 239uy; 130uy; 49uy; 179uy; 130uy; 61uy; 238uy; 32uy; 105uy; 91uy; 179uy; 84uy; 142uy; 104uy; 209uy; 125uy; 254uy; 222uy; 230uy; 177uy; 63uy; 110uy; 114uy; 39uy; 191uy; 144uy; 172uy; 242uy; 109uy; 55uy; 178uy; 175uy; 201uy; 104uy; 106uy; 150uy; 54uy; 59uy; 95uy; 44uy; 125uy; 248uy; 160uy; 201uy; 159uy; 109uy; 2uy; 204uy; 238uy; 71uy; 50uy; 57uy; 50uy; 47uy; 163uy; 219uy; 26uy; 32uy; 44uy; 12uy; 198uy; 167uy; 22uy; 12uy; 49uy; 4uy; 215uy; 15uy; 136uy; 68uy; 238uy; 223uy; 47uy; 146uy; 33uy; 222uy; 229uy; 139uy; 103uy; 117uy; 145uy; 192uy; 97uy; 77uy; 145uy; 62uy; 109uy; 181uy; 52uy; 169uy; 89uy; 36uy; 72uy; 83uy; 197uy; 38uy; 97uy; 8uy; 88uy; 237uy; 171uy; 171uy; 184uy; 183uy; 23uy; 159uy; 85uy; 46uy; 95uy; 24uy; 197uy; 225uy; 24uy; 135uy; 225uy; 247uy; 67uy; 51uy; 204uy; 20uy; 140uy; 110uy; 152uy; 147uy; 142uy; 144uy; 83uy; 53uy; 83uy; 101uy; 75uy; 91uy; 196uy; 84uy; 254uy; 156uy; 147uy; 125uy; 162uy; 255uy; 0uy; 187uy; 254uy; 254uy; 117uy; 6uy; 130uy; 155uy; 55uy; 76uy; 113uy; 245uy; 134uy; 249uy; 178uy; 178uy; 92uy; 138uy; 109uy; 60uy; 17uy; 188uy; 244uy; 97uy; 130uy; 146uy; 229uy; 186uy; 158uy; 56uy; 93uy; 61uy; 47uy; 49uy; 156uy; 156uy; 46uy; 18uy; 18uy; 67uy; 56uy; 168uy; 98uy; 13uy; 236uy; 251uy; 213uy; 84uy; 187uy; 177uy; 173uy; 145uy; 211uy; 133uy; 252uy; 171uy; 106uy; 169uy; 47uy; 196uy; 49uy; 79uy; 164uy; 199uy; 115uy; 221uy; 117uy; 92uy; 185uy; 149uy; 160uy; 13uy; 119uy; 187uy; 92uy; 193uy; 34uy; 18uy; 150uy; 150uy; 134uy; 186uy; 177uy; 145uy; 215uy; 129uy; 2uy; 216uy; 117uy; 40uy; 254uy; 234uy; 59uy; 188uy; 82uy; 129uy; 89uy; 24uy; 116uy; 253uy; 56uy; 100uy; 80uy; 226uy; 118uy; 37uy; 226uy; 248uy; 155uy; 236uy; 20uy; 198uy; 227uy; 100uy; 12uy; 156uy; 242uy; 68uy; 216uy; 143uy; 39uy; 114uy; 60uy; 101uy; 170uy; 201uy; 168uy; 50uy; 58uy; 221uy; 226uy; 242uy; 112uy; 208uy; 237uy; 19uy; 84uy; 131uy; 65uy; 169uy; 25uy; 222uy; 105uy; 14uy; 93uy; 2uy; 155uy; 26uy; 126uy; 154uy; 86uy; 72uy; 134uy; 34uy; 128uy; 31uy; 82uy; 184uy; 19uy; 68uy; 84uy; 173uy; 106uy; 181uy; 69uy; 47uy; 131uy; 184uy; 182uy; 166uy; 190uy; 174uy; 11uy; 69uy; 191uy; 177uy; 160uy; 161uy; 11uy; 237uy; 254uy; 92uy; 151uy; 223uy; 206uy; 169uy; 34uy; 100uy; 196uy; 110uy; 155uy; 89uy; 98uy; 213uy; 86uy; 214uy; 86uy; 20uy; 151uy; 150uy; 182uy; 76uy; 117uy; 77uy; 108uy; 203uy; 23uy; 67uy; 60uy; 116uy; 165uy; 52uy; 66uy; 153uy; 34uy; 4uy; 221uy; 164uy; 113uy; 129uy; 248uy; 251uy; 156uy; 81uy; 234uy; 55uy; 163uy; 119uy; 226uy; 237uy; 180uy; 75uy; 168uy; 125uy; 50uy; 109uy; 100uy; 178uy; 106uy; 182uy; 147uy; 103uy; 36uy; 185uy; 148uy; 56uy; 104uy; 145uy; 141uy; 140uy; 229uy; 126uy; 28uy; 2uy; 9uy; 165uy; 243uy; 60uy; 18uy; 192uy; 175uy; 35uy; 12uy; 57uy; 69uy; 112uy; 220uy; 210uy; 72uy; 237uy; 156uy; 67uy; 27uy; 66uy; 136uy; 156uy; 136uy; 226uy; 245uy; 13uy; 175uy; 85uy; 150uy; 78uy; 190uy; 102uy; 140uy; 105uy; 190uy; 113uy; 21uy; 247uy; 16uy; 71uy; 6uy; 19uy; 83uy; 17uy; 200uy; 99uy; 87uy; 209uy; 185uy; 240uy; 153uy; 18uy; 101uy; 128uy; 157uy; 38uy; 188uy; 36uy; 44uy; 151uy; 42uy; 18uy; 68uy; 96uy; 136uy; 61uy; 134uy; 73uy; 122uy; 20uy; 143uy; 115uy; 81uy; 68uy; 238uy; 156uy; 107uy; 172uy; 42uy; 180uy; 162uy; 158uy; 21uy; 164uy; 9uy; 181uy; 147uy; 7uy; 99uy; 114uy; 231uy; 196uy; 176uy; 137uy; 34uy; 20uy; 166uy; 53uy; 242uy; 247uy; 99uy; 157uy; 30uy; 72uy; 196uy; 86uy; 181uy; 233uy; 237uy; 142uy; 86uy; 108uy; 228uy; 246uy; 213uy; 84uy; 249uy; 255uy; 196uy; 0uy; 44uy; 17uy; 0uy; 2uy; 1uy; 2uy; 5uy; 3uy; 3uy; 3uy; 5uy; 1uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 1uy; 2uy; 3uy; 4uy; 17uy; 5uy; 18uy; 19uy; 33uy; 34uy; 0uy; 6uy; 49uy; 20uy; 65uy; 81uy; 35uy; 50uy; 97uy; 53uy; 67uy; 115uy; 145uy; 177uy; 178uy; 255uy; 218uy; 0uy; 8uy; 1uy; 2uy; 1uy; 1uy; 63uy; 1uy; 196uy; 177uy; 9uy; 104uy; 39uy; 161uy; 58uy; 98uy; 74uy; 105uy; 154uy; 84uy; 169uy; 228uy; 138uy; 241uy; 143uy; 165uy; 146uy; 84uy; 206uy; 203uy; 155uy; 37uy; 219uy; 50uy; 139uy; 230uy; 95uy; 131uy; 110uy; 150uy; 174uy; 149uy; 254uy; 202uy; 154uy; 118uy; 246uy; 227uy; 52uy; 103uy; 252uy; 110uy; 170uy; 43uy; 41uy; 233uy; 145uy; 154uy; 89uy; 80uy; 16uy; 172uy; 203uy; 30uy; 116uy; 18uy; 73uy; 148uy; 125uy; 177uy; 171uy; 50uy; 220uy; 147uy; 97uy; 230uy; 215uy; 59uy; 144uy; 58uy; 194uy; 170uy; 166uy; 172uy; 165uy; 245uy; 19uy; 160uy; 141uy; 222uy; 89uy; 109uy; 26uy; 178uy; 184uy; 72uy; 195uy; 90uy; 49uy; 153uy; 73uy; 12uy; 114uy; 216uy; 147uy; 238uy; 73uy; 216uy; 117uy; 222uy; 202uy; 93uy; 112uy; 181uy; 93uy; 217uy; 165uy; 168uy; 80uy; 7uy; 147uy; 113uy; 7uy; 206uy; 223uy; 222uy; 221uy; 36uy; 213uy; 120uy; 108uy; 145uy; 213uy; 83uy; 199uy; 150uy; 158uy; 55uy; 23uy; 81uy; 83uy; 172uy; 146uy; 53uy; 142uy; 95uy; 83uy; 163uy; 49uy; 85uy; 144uy; 252uy; 0uy; 139uy; 113uy; 178uy; 237uy; 126uy; 153uy; 106uy; 42uy; 204uy; 179uy; 84uy; 37uy; 218uy; 167uy; 60uy; 208uy; 179uy; 84uy; 164uy; 114uy; 6uy; 114uy; 72uy; 210uy; 138uy; 105uy; 134uy; 164uy; 78uy; 252uy; 79uy; 18uy; 214uy; 251uy; 9uy; 35uy; 43uy; 118uy; 159uy; 232uy; 176uy; 255uy; 0uy; 52uy; 255uy; 0uy; 247uy; 215uy; 114uy; 243uy; 151uy; 9uy; 167uy; 201uy; 157uy; 234uy; 170uy; 94uy; 24uy; 198uy; 104uy; 21uy; 3uy; 185uy; 129uy; 84uy; 201uy; 175uy; 75uy; 84uy; 182uy; 187uy; 13uy; 194uy; 130uy; 63uy; 59uy; 116uy; 123uy; 98uy; 169uy; 166uy; 137uy; 253uy; 21uy; 22uy; 146uy; 147uy; 173uy; 25uy; 173uy; 144uy; 25uy; 238uy; 54uy; 222uy; 26uy; 120uy; 35uy; 136uy; 47uy; 144uy; 18uy; 31uy; 62uy; 73uy; 91uy; 40uy; 131uy; 182uy; 42uy; 209uy; 108uy; 244uy; 148uy; 90uy; 153uy; 141uy; 165uy; 74uy; 178uy; 108uy; 132uy; 220uy; 46uy; 149uy; 85uy; 61uy; 76uy; 92uy; 60uy; 102uy; 182uy; 226uy; 215uy; 228uy; 9uy; 61uy; 182uy; 233uy; 38uy; 21uy; 19uy; 34uy; 149uy; 93uy; 89uy; 128uy; 7uy; 79uy; 107uy; 61uy; 191uy; 106uy; 56uy; 163uy; 183uy; 198uy; 84uy; 31uy; 158uy; 187uy; 186uy; 58uy; 166uy; 56uy; 92uy; 180uy; 145uy; 75uy; 36uy; 144uy; 77uy; 52uy; 160uy; 197uy; 27uy; 62uy; 70uy; 93uy; 6uy; 66uy; 114uy; 131uy; 109uy; 198uy; 215uy; 243uy; 99uy; 111uy; 7uy; 170uy; 140uy; 70uy; 187uy; 16uy; 142uy; 156uy; 193uy; 61uy; 94uy; 13uy; 50uy; 43uy; 122uy; 136uy; 223uy; 15uy; 169uy; 153uy; 37uy; 115uy; 150uy; 197uy; 37uy; 138uy; 9uy; 184uy; 45uy; 154uy; 202uy; 193uy; 79uy; 46uy; 87uy; 33uy; 109uy; 54uy; 41uy; 93uy; 77uy; 134uy; 26uy; 72uy; 150uy; 183uy; 18uy; 175uy; 101uy; 145uy; 13uy; 111uy; 163uy; 168uy; 138uy; 52uy; 89uy; 25uy; 172uy; 223uy; 90uy; 52uy; 119uy; 120uy; 208uy; 229uy; 78uy; 54uy; 184uy; 82uy; 73uy; 2uy; 199uy; 181uy; 226uy; 146uy; 28uy; 34uy; 24uy; 229uy; 141uy; 226uy; 113uy; 44uy; 215uy; 73uy; 21uy; 145uy; 135uy; 61uy; 184uy; 176uy; 6uy; 199uy; 216uy; 251uy; 251uy; 117uy; 255uy; 196uy; 0uy; 43uy; 16uy; 0uy; 2uy; 2uy; 2uy; 1uy; 2uy; 5uy; 4uy; 1uy; 5uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 2uy; 3uy; 1uy; 4uy; 5uy; 17uy; 18uy; 6uy; 19uy; 0uy; 20uy; 33uy; 34uy; 49uy; 21uy; 35uy; 50uy; 65uy; 22uy; 37uy; 81uy; 83uy; 97uy; 114uy; 255uy; 218uy; 0uy; 8uy; 1uy; 1uy; 0uy; 6uy; 63uy; 2uy; 195uy; 100uy; 83uy; 101uy; 164uy; 143uy; 161uy; 106uy; 222uy; 35uy; 139uy; 217uy; 91uy; 41uy; 95uy; 234uy; 15uy; 239uy; 6uy; 148uy; 182uy; 69uy; 123uy; 233uy; 142uy; 39uy; 66uy; 209uy; 106uy; 33uy; 156uy; 146uy; 216uy; 58uy; 238uy; 103uy; 20uy; 185uy; 189uy; 81uy; 70uy; 175uy; 121uy; 64uy; 216uy; 85uy; 193uy; 177uy; 89uy; 225uy; 4uy; 59uy; 226uy; 213uy; 53uy; 48uy; 75uy; 48uy; 252uy; 76uy; 103uy; 241uy; 40uy; 152uy; 253uy; 120uy; 185uy; 115uy; 15uy; 158uy; 165uy; 212uy; 54uy; 208uy; 48uy; 40uy; 161uy; 140uy; 38uy; 188uy; 221uy; 101uy; 179uy; 193uy; 34uy; 230uy; 41uy; 44uy; 138uy; 213uy; 224uy; 189uy; 246uy; 108uy; 16uy; 151uy; 106uy; 184uy; 48uy; 193uy; 110uy; 108uy; 2uy; 89uy; 212uy; 121uy; 43uy; 217uy; 6uy; 100uy; 108uy; 63uy; 63uy; 36uy; 109uy; 32uy; 114uy; 82uy; 173uy; 208uy; 167uy; 62uy; 86uy; 157uy; 103uy; 68uy; 21uy; 106uy; 85uy; 247uy; 219uy; 174uy; 175uy; 89uy; 212uy; 75uy; 24uy; 108uy; 115uy; 24uy; 194uy; 171uy; 103uy; 184uy; 61uy; 144uy; 233uy; 184uy; 103uy; 115uy; 124uy; 151uy; 161uy; 200uy; 91uy; 157uy; 236uy; 57uy; 110uy; 63uy; 231uy; 115uy; 253uy; 189uy; 124uy; 89uy; 197uy; 100uy; 109uy; 247uy; 114uy; 86uy; 18uy; 92uy; 26uy; 204uy; 80uy; 211uy; 177uy; 85uy; 113uy; 169uy; 100uy; 99uy; 124uy; 237uy; 48uy; 115uy; 43uy; 132uy; 207uy; 169uy; 17uy; 184uy; 166uy; 11uy; 222uy; 113uy; 203uy; 94uy; 42uy; 209uy; 198uy; 88uy; 33uy; 94uy; 50uy; 17uy; 78uy; 234uy; 149uy; 140uy; 115uy; 235uy; 146uy; 146uy; 34uy; 7uy; 54uy; 237uy; 210uy; 164uy; 126uy; 94uy; 218uy; 83uy; 182uy; 134uy; 216uy; 43uy; 223uy; 163uy; 130uy; 0uy; 251uy; 161uy; 212uy; 37uy; 51uy; 26uy; 156uy; 210uy; 245uy; 51uy; 62uy; 147uy; 253uy; 54uy; 159uy; 196uy; 207uy; 250uy; 241uy; 111uy; 34uy; 191uy; 163uy; 99uy; 169uy; 85uy; 233uy; 251uy; 25uy; 45uy; 98uy; 177uy; 185uy; 90uy; 214uy; 188uy; 154uy; 45uy; 220uy; 62uy; 45uy; 42uy; 125uy; 65uy; 74uy; 31uy; 107uy; 182uy; 19uy; 201uy; 129uy; 229uy; 212uy; 95uy; 226uy; 8uy; 141uy; 248uy; 185uy; 95uy; 249uy; 157uy; 105uy; 188uy; 192uy; 84uy; 210uy; 178uy; 181uy; 227uy; 30uy; 172uy; 127uy; 40uy; 6uy; 22uy; 252uy; 229uy; 252uy; 149uy; 139uy; 102uy; 113uy; 50uy; 183uy; 119uy; 174uy; 232uy; 127uy; 1uy; 0uy; 104uy; 147uy; 73uy; 183uy; 147uy; 213uy; 29uy; 234uy; 169uy; 242uy; 224uy; 120uy; 229uy; 46uy; 172uy; 220uy; 55uy; 28uy; 72uy; 19uy; 212uy; 204uy; 70uy; 71uy; 23uy; 116uy; 192uy; 248uy; 114uy; 108uy; 65uy; 20uy; 38uy; 103uy; 230uy; 22uy; 98uy; 3uy; 158uy; 110uy; 102uy; 150uy; 19uy; 36uy; 145uy; 188uy; 0uy; 38uy; 234uy; 215uy; 221uy; 122uy; 29uy; 52uy; 233uy; 28uy; 109uy; 183uy; 175uy; 220uy; 142uy; 199uy; 110uy; 75uy; 144uy; 206uy; 219uy; 221uy; 212uy; 247uy; 56uy; 71uy; 31uy; 21uy; 170uy; 229uy; 173uy; 211uy; 171uy; 94uy; 255uy; 0uy; 76uy; 157uy; 82uy; 139uy; 118uy; 83uy; 90uy; 28uy; 182uy; 93uy; 182uy; 14uy; 0uy; 239uy; 24uy; 114uy; 208uy; 51uy; 221uy; 199uy; 124uy; 121uy; 70uy; 254uy; 99uy; 195uy; 198uy; 207uy; 87uy; 224uy; 157uy; 200uy; 190uy; 201uy; 217uy; 179uy; 94uy; 86uy; 10uy; 18uy; 45uy; 123uy; 171uy; 95uy; 238uy; 119uy; 8uy; 101uy; 124uy; 254uy; 196uy; 251uy; 132uy; 184uy; 204uy; 15uy; 205uy; 75uy; 147uy; 213uy; 216uy; 38uy; 227uy; 210uy; 245uy; 186uy; 205uy; 37uy; 89uy; 168uy; 149uy; 183uy; 176uy; 33uy; 32uy; 149uy; 17uy; 221uy; 97uy; 200uy; 61uy; 163uy; 183uy; 201uy; 45uy; 95uy; 110uy; 72uy; 67uy; 223uy; 59uy; 30uy; 161uy; 125uy; 75uy; 8uy; 180uy; 146uy; 205uy; 140uy; 67uy; 171uy; 57uy; 111uy; 84uy; 204uy; 99uy; 169uy; 238uy; 33uy; 138uy; 34uy; 13uy; 199uy; 238uy; 55uy; 184uy; 253uy; 248uy; 255uy; 196uy; 0uy; 27uy; 16uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 1uy; 17uy; 33uy; 0uy; 49uy; 97uy; 65uy; 81uy; 255uy; 218uy; 0uy; 8uy; 1uy; 1uy; 0uy; 1uy; 63uy; 33uy; 165uy; 40uy; 98uy; 156uy; 1uy; 235uy; 101uy; 220uy; 211uy; 202uy; 45uy; 71uy; 118uy; 227uy; 36uy; 122uy; 255uy; 0uy; 121uy; 75uy; 247uy; 255uy; 0uy; 3uy; 24uy; 44uy; 189uy; 46uy; 105uy; 228uy; 116uy; 168uy; 10uy; 144uy; 180uy; 93uy; 36uy; 175uy; 42uy; 144uy; 170uy; 106uy; 84uy; 95uy; 47uy; 225uy; 80uy; 112uy; 160uy; 175uy; 243uy; 15uy; 202uy; 117uy; 68uy; 244uy; 247uy; 16uy; 150uy; 221uy; 213uy; 49uy; 110uy; 87uy; 249uy; 32uy; 150uy; 110uy; 105uy; 30uy; 7uy; 244uy; 124uy; 226uy; 102uy; 2uy; 249uy; 46uy; 166uy; 211uy; 76uy; 11uy; 21uy; 214uy; 32uy; 112uy; 106uy; 41uy; 37uy; 199uy; 132uy; 179uy; 26uy; 152uy; 137uy; 116uy; 248uy; 45uy; 59uy; 209uy; 252uy; 42uy; 141uy; 201uy; 201uy; 130uy; 254uy; 44uy; 78uy; 43uy; 50uy; 207uy; 35uy; 181uy; 96uy; 240uy; 49uy; 237uy; 115uy; 246uy; 109uy; 116uy; 226uy; 28uy; 175uy; 113uy; 239uy; 2uy; 115uy; 176uy; 236uy; 17uy; 76uy; 40uy; 88uy; 230uy; 21uy; 60uy; 160uy; 81uy; 130uy; 124uy; 43uy; 112uy; 15uy; 127uy; 255uy; 218uy; 0uy; 12uy; 3uy; 1uy; 0uy; 2uy; 0uy; 3uy; 0uy; 0uy; 0uy; 16uy; 101uy; 213uy; 70uy; 7uy; 38uy; 136uy; 255uy; 196uy; 0uy; 25uy; 17uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 1uy; 17uy; 0uy; 49uy; 33uy; 65uy; 255uy; 218uy; 0uy; 8uy; 1uy; 3uy; 1uy; 1uy; 63uy; 16uy; 56uy; 85uy; 183uy; 128uy; 229uy; 151uy; 192uy; 201uy; 77uy; 241uy; 116uy; 1uy; 134uy; 36uy; 100uy; 64uy; 134uy; 39uy; 70uy; 76uy; 235uy; 51uy; 30uy; 154uy; 59uy; 89uy; 243uy; 154uy; 134uy; 219uy; 196uy; 87uy; 204uy; 137uy; 84uy; 192uy; 172uy; 177uy; 240uy; 162uy; 58uy; 158uy; 22uy; 32uy; 180uy; 28uy; 108uy; 147uy; 149uy; 132uy; 236uy; 57uy; 53uy; 47uy; 195uy; 185uy; 148uy; 103uy; 167uy; 100uy; 8uy; 52uy; 101uy; 104uy; 29uy; 224uy; 50uy; 8uy; 112uy; 4uy; 29uy; 163uy; 187uy; 37uy; 207uy; 163uy; 20uy; 148uy; 81uy; 215uy; 75uy; 76uy; 221uy; 1uy; 240uy; 253uy; 45uy; 107uy; 203uy; 62uy; 172uy; 171uy; 24uy; 224uy; 184uy; 68uy; 15uy; 118uy; 110uy; 199uy; 235uy; 116uy; 15uy; 97uy; 81uy; 40uy; 90uy; 218uy; 164uy; 247uy; 148uy; 95uy; 236uy; 20uy; 41uy; 15uy; 69uy; 223uy; 30uy; 142uy; 247uy; 193uy; 152uy; 76uy; 89uy; 16uy; 97uy; 220uy; 56uy; 134uy; 233uy; 236uy; 216uy; 164uy; 243uy; 213uy; 228uy; 222uy; 151uy; 146uy; 251uy; 193uy; 191uy; 255uy; 196uy; 0uy; 27uy; 17uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 1uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 1uy; 17uy; 33uy; 0uy; 49uy; 65uy; 97uy; 81uy; 255uy; 218uy; 0uy; 8uy; 1uy; 2uy; 1uy; 1uy; 63uy; 16uy; 3uy; 96uy; 31uy; 15uy; 184uy; 201uy; 238uy; 80uy; 34uy; 223uy; 132uy; 175uy; 244uy; 46uy; 134uy; 145uy; 68uy; 99uy; 187uy; 231uy; 81uy; 36uy; 83uy; 20uy; 240uy; 108uy; 112uy; 65uy; 176uy; 144uy; 239uy; 54uy; 1uy; 229uy; 200uy; 13uy; 21uy; 33uy; 0uy; 34uy; 82uy; 160uy; 32uy; 0uy; 80uy; 42uy; 154uy; 129uy; 138uy; 134uy; 246uy; 230uy; 91uy; 235uy; 2uy; 69uy; 26uy; 155uy; 161uy; 158uy; 70uy; 18uy; 139uy; 136uy; 247uy; 33uy; 6uy; 66uy; 57uy; 119uy; 218uy; 71uy; 81uy; 244uy; 98uy; 143uy; 237uy; 247uy; 247uy; 136uy; 43uy; 98uy; 145uy; 68uy; 36uy; 24uy; 14uy; 13uy; 180uy; 13uy; 42uy; 109uy; 218uy; 13uy; 236uy; 163uy; 133uy; 181uy; 238uy; 97uy; 208uy; 49uy; 142uy; 130uy; 104uy; 210uy; 32uy; 139uy; 28uy; 57uy; 36uy; 45uy; 149uy; 50uy; 0uy; 203uy; 223uy; 13uy; 169uy; 94uy; 73uy; 63uy; 3uy; 108uy; 152uy; 187uy; 216uy; 251uy; 216uy; 37uy; 227uy; 35uy; 214uy; 200uy; 66uy; 230uy; 68uy; 49uy; 203uy; 182uy; 183uy; 113uy; 5uy; 248uy; 249uy; 46uy; 128uy; 169uy; 74uy; 240uy; 73uy; 10uy; 96uy; 13uy; 132uy; 26uy; 145uy; 30uy; 255uy; 196uy; 0uy; 24uy; 16uy; 1uy; 1uy; 1uy; 1uy; 1uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 1uy; 17uy; 0uy; 33uy; 49uy; 255uy; 218uy; 0uy; 8uy; 1uy; 1uy; 0uy; 1uy; 63uy; 16uy; 73uy; 4uy; 0uy; 38uy; 88uy; 17uy; 199uy; 56uy; 118uy; 113uy; 172uy; 180uy; 165uy; 110uy; 169uy; 136uy; 42uy; 56uy; 146uy; 64uy; 218uy; 120uy; 25uy; 254uy; 49uy; 255uy; 0uy; 167uy; 163uy; 46uy; 170uy; 6uy; 73uy; 49uy; 132uy; 47uy; 54uy; 205uy; 246uy; 136uy; 24uy; 52uy; 16uy; 169uy; 133uy; 1uy; 89uy; 8uy; 70uy; 85uy; 42uy; 216uy; 17uy; 14uy; 131uy; 148uy; 157uy; 33uy; 164uy; 61uy; 229uy; 230uy; 95uy; 216uy; 98uy; 161uy; 160uy; 162uy; 0uy; 109uy; 136uy; 76uy; 182uy; 231uy; 156uy; 22uy; 32uy; 130uy; 198uy; 48uy; 196uy; 39uy; 41uy; 75uy; 84uy; 30uy; 194uy; 236uy; 28uy; 48uy; 195uy; 209uy; 190uy; 163uy; 198uy; 219uy; 180uy; 27uy; 117uy; 64uy; 93uy; 154uy; 61uy; 88uy; 116uy; 73uy; 1uy; 244uy; 236uy; 81uy; 164uy; 133uy; 180uy; 1uy; 196uy; 179uy; 10uy; 96uy; 219uy; 112uy; 132uy; 25uy; 96uy; 78uy; 159uy; 171uy; 177uy; 234uy; 81uy; 114uy; 142uy; 144uy; 228uy; 72uy; 91uy; 58uy; 159uy; 215uy; 121uy; 195uy; 180uy; 223uy; 255uy; 217uy; |]

let tags = [| "raft"; "iris"; "remote"; "git"; "store"; "persistence"; "frontend"; "yaml" |]
let tiers = [| Tier.FrontEnd; Tier.Client; Tier.Service |]
let levels = [| LogLevel.Debug; LogLevel.Info; LogLevel.Warn; LogLevel.Err; LogLevel.Trace |]

let rnd = Random()
let oneOf (ar: 'T[]) =
  ar.[rnd.Next(ar.Length)]

let genLog() =
  { Time = rnd.Next() |> uint32
    Thread = rnd.Next()
    Tier = oneOf tiers
    MachineId = IrisId.Create()
    Tag = oneOf tags
    LogLevel = oneOf levels
    Message = oneOf loremIpsum }

let props =
  [| { Key = "1"; Value = "One" }
     { Key = "2"; Value = "Two" }
     { Key = "3"; Value = "Three" }
     { Key = "4"; Value = "Four" }
     { Key = "5"; Value = "Five" } |]

let rndColor () =
  { Red = byte <| rnd.Next(0,255)
    Green = byte <| rnd.Next(0,255)
    Blue = byte <| rnd.Next(0,255)
    Alpha = byte <| rnd.Next(0,255) }
  |> ColorSpace.RGBA

let pins groupId =
  let clientId = ClientId.Create()
  [ Pin.Sink.bang      (PinId.Create()) (name "Bang")      groupId clientId [| false |]
    Pin.Sink.toggle    (PinId.Create()) (name "Toggle")    groupId clientId [| true  |]
    Pin.Sink.fileName  (PinId.Create()) (name "FileName")  groupId clientId [| "/dev/null"      |]
    Pin.Sink.directory (PinId.Create()) (name "Directory") groupId clientId [| "/dev"           |]
    Pin.Sink.ip        (PinId.Create()) (name "IP")        groupId clientId [| "127.0.0.1"      |]
    Pin.Sink.url       (PinId.Create()) (name "URL")       groupId clientId [| "ftp://dev.null" |]
    Pin.Sink.string    (PinId.Create()) (name "String")    groupId clientId [| "Hello!" |]
    Pin.Sink.multiLine (PinId.Create()) (name "MultiLine") groupId clientId [| text |]
    Pin.Sink.number    (PinId.Create()) (name "Number")    groupId clientId [| 666. |]
    Pin.Sink.bytes     (PinId.Create()) (name "Bytes")     groupId clientId [| image |]
    Pin.Sink.color     (PinId.Create()) (name "Color")     groupId clientId [| rndColor() |]
    Pin.Sink.enum      (PinId.Create()) (name "Enum")      groupId clientId props [| props.[0] |] ]
  |> List.map (fun pin -> pin.Id, pin)
  |> Map.ofList

let pinGroups () =
  List.fold
    (fun lst n ->
      let group = PinGroup.create (name ("Group " + string n))
      let pins = pins group.Id
      PinGroup.setPins pins group :: lst)
    List.empty
    [ 1 .. 3 ]

let makeClient service (name: Name) : IrisClient =
  { Id = IrisId.Create()
    Name = name
    Role = Role.Renderer
    ServiceId = service
    Status = ServiceStatus.Running
    IpAddress = IpAddress.Localhost
    Port = port 5000us }

let machines =
  List.map (fun idx ->
    { MachineId        = IrisId.Create()
      HostName         = name ("mockmachine-" + string idx)
      WorkSpace        = filepath "/Iris"
      LogDirectory     = filepath "/Iris"
      AssetDirectory   = filepath "/Iris"
      AssetFilter      = Constants.DEFAULT_ASSET_FILTER
      BindAddress      = IPv4Address "127.0.0.1"
      MulticastAddress = IpAddress.Parse Constants.MCAST_ADDRESS
      MulticastPort    = port Constants.MCAST_PORT
      WebPort          = port Constants.DEFAULT_WEB_PORT
      RaftPort         = port Constants.DEFAULT_RAFT_PORT
      WsPort           = port Constants.DEFAULT_WEB_SOCKET_PORT
      GitPort          = port Constants.DEFAULT_GIT_PORT
      ApiPort          = port Constants.DEFAULT_API_PORT
      Version          = version "0.0.0" })
    [ 0 .. 3 ]

let clients =
  Seq.map
    (fun (service:IrisMachine) ->
      makeClient service.MachineId (sprintf "%A Client" service.HostName |> name))
    machines
  |> List.ofSeq

let makeTree (machine:IrisMachine) =
  let randomFileName () = IrisId.Create().Prefix() |> filepath
  let makeDir (fsPath:FsPath) =
    FsEntry.Directory(
      { Path = fsPath
        Name = FsPath.fileName fsPath
        MimeType = "application/x-directory"
        Size = 0u
        Filtered = 0u
      }, Map.empty)
  let makeFile (fsPath:FsPath) =
    FsEntry.File(
      { Path = fsPath
        Name = FsPath.fileName fsPath
        MimeType = "text/plain"
        Size = 0u
        Filtered = 0u
      })

  let rootPath = {
    Drive = 'C'
    Platform = Windows
    Elements = [ "Iris"; "Assets" ]
  }

  let addChild dir child =
    FsEntry.modify (FsEntry.path dir) (FsEntry.addChild child) dir

  let root =
    let dirPath1 = rootPath + randomFileName()
    let dirPath2 = rootPath + randomFileName()
    let dirPath3 = rootPath + randomFileName()
    let subdirPath1 = dirPath1 + randomFileName()
    let subdirPath2 = dirPath2 + randomFileName()
    let subdirPath3 = dirPath3 + randomFileName()
    let filePath1 = subdirPath1 + randomFileName()
    let filePath2 = subdirPath2 + randomFileName()
    let filePath3 = subdirPath2 + randomFileName()
    let filePath4 = subdirPath1 + randomFileName()
    let filePath5 = subdirPath2 + randomFileName()
    let filePath6 = subdirPath2 + randomFileName()

    let dir1 = makeDir dirPath1
    let dir2 = makeDir dirPath2
    let dir3 = makeDir dirPath3
    let subdir1 = makeDir subdirPath1
    let subdir2 = makeDir subdirPath2
    let subdir3 = makeDir subdirPath3
    let file1 = makeFile filePath1
    let file2 = makeFile filePath2
    let file3 = makeFile filePath3
    let file4 = makeFile filePath4
    let file5 = makeFile filePath5
    let file6 = makeFile filePath6

    FsEntry.Directory(
      { Path = rootPath
        Name = FsPath.fileName rootPath
        MimeType = "application/x-directory"
        Size = 0u
        Filtered = 0u
      },Map [
        dirPath1, addChild dir1 (addChild subdir1 file1 |> fun subdir -> addChild subdir file4)
        dirPath2, addChild dir2 (addChild subdir2 file2 |> fun subdir -> addChild subdir file5)
        dirPath3, addChild dir3 (addChild subdir3 file3 |> fun subdir -> addChild subdir file6)
      ])
  { HostId = machine.MachineId; Root = root; Filters = Array.empty }

let trees =
  machines
  |> Seq.map (fun machine -> machine.MachineId, makeTree machine)
  |> Map.ofSeq

let project =
    let members =
      List.map
        (fun (machine: IrisMachine) ->
          let mem = { Iris.Raft.Member.create machine.MachineId with HostName = machine.HostName }
          (mem.Id, mem))
        machines
      |> Map.ofList
    let machine = machines.[0]
    let clusterConfig =
      { Id = IrisId.Create()
        Name = name "mockcluster"
        Members = members
        Groups = [||] }
    let irisConfig =
        { Machine    = machine
          ActiveSite = Some clusterConfig.Id
          Version   = "0.0.0"
          Audio     = AudioConfig.Default
          Clients   = ClientConfig.Default
          Raft      = RaftConfig.Default
          Timing    = TimingConfig.Default
          Sites     = [| clusterConfig |] }
    { Id        = IrisId.Create()
      Name      = name "mockproject"
      Path      = filepath "/Iris/mockproject"
      CreatedOn = Time.createTimestamp()
      LastSaved = Some (Time.createTimestamp ())
      Copyright = None
      Author    = None
      Config    = irisConfig  }

let _1of4 (x,_,_,_) = x
let _2of4 (_,x,_,_) = x
let _3of4 (_,_,x,_) = x
let _4of4 (_,_,_,x) = x

let cuesAndListsAndPlayers =
  let makeCue i =
    // Create new Cue and CueReference
    let cue = Cue.create ("Cue " + string i) [| |]
    let cueRef = CueReference.ofCue cue
    cue, cueRef
  let cue1, cueRef1 = makeCue 1
  let cue2, cueRef2 = makeCue 2
  let cue3, cueRef3 = makeCue 3
  let cueGroup =
    CueGroup.create [|
      cueRef1
      cueRef2
      cueRef3
    |]
  let cueList =
    CueList.create "mockcuelist" [|
      cueGroup
    |]
  let cuePlayer = CuePlayer.create "mockcueplayer" (Some cueList.Id)
  let playerGroup = PinGroup.ofPlayer cuePlayer
  Map[cue1.Id, cue1; cue2.Id, cue2; cue3.Id, cue3],
  Map[cueList.Id, cueList],
  Map[cuePlayer.Id, cuePlayer],
  playerGroup

let getMockState() =
  let initial = pinGroups()
  let groups =
    clients
    |> List.collect
      (fun client ->
        List.map
          (fun (group:PinGroup) ->
            group
            |> PinGroup.setClientId client.Id
            |> PinGroup.map (Pin.setClient client.Id))
          initial)
    |> PinGroupMap.ofSeq
  let clients =
    clients
    |> List.map (fun client -> client.Id, client)
    |> Map.ofList
  { State.Empty with
      FsTrees = trees
      Project = project
      Clients = clients
      PinGroups = groups }
