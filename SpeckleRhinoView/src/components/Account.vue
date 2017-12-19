<template>
  <v-card class='account-content receiver-content'>
    <v-card-text>
      <v-layout>
        <v-flex xs2>
          <v-icon>vpn_key</v-icon>
        </v-flex>
        <v-flex xs10><code class="grey darken-2 white--text ellipsis">{{ account.apiToken }}</code></v-flex>
      </v-layout>
      <v-layout>
        <v-flex xs2>
          <v-icon>link</v-icon>
        </v-flex>
        <v-flex xs10><code class="grey darken-2 white--text ellipsis">{{ account.restApi }}</code></v-flex>
      </v-layout>
    </v-card-text>
    <v-card-actions>
      <v-btn icon small @click=''>
        <v-icon>down</v-icon>
      </v-btn>
      <v-spacer></v-spacer>
      <v-btn small @click='pingDialog=true'>Get Streams</v-btn>
      <v-btn small color='red' dark @click='deleteDialog=true'>delete</v-btn>
    </v-card-actions>
    <v-dialog v-model="deleteDialog" persistent>
      <v-card>
        <v-card-title class="headline">Are you sure?</v-card-title>
        <v-card-text>This will permanently delete this account, and there's no undo button!</v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn flat @click.native="deleteDialog=false">Cancel</v-btn>
          <v-btn color="red" @click.native="deleteAccount">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
    <v-dialog v-model='pingDialog' fullscreen>
      <v-card>
        <v-toolbar style="flex: 0 0 auto;" dark>
          <v-btn icon @click.native="pingDialog=false" dark class='mr-0'>
            <v-icon>close</v-icon>
          </v-btn>
          <v-toolbar-title>{{account.serverName}} / {{account.email}}</v-toolbar-title>
        </v-toolbar>
        <v-card-text>
          <div v-if='streams.length==0&&!fail'>No streams found for this account.<span class='caption'>({{account.serverName}})</span></div>
          <div v-if='!fail'>
            <v-layout>
              <div class='title ml-3 mb-2'>
                Here are your {{ streams.length }} streams:
              </div>
            </v-layout>
            <v-layout style='height: 75vh; overflow-y: scroll; position: relative; overflow-y:scroll; overflow-x:hidden;'>
              <v-list>
                <template v-for='stream in streams'>
                  <v-list-tile>
                    <v-list-tile-content>
                      <v-list-tile-title>
                      <div class='subheading'>{{stream.name}}</div>
                      </v-list-tile-title>
                      <v-list-tile-sub-title class='caption'><code>{{stream.streamId}}</code> | Last update:
                        <timeago :auto-update='10000000' :since='stream.updatedAt'></timeago>
                      </v-list-tile-sub-title>
                    </v-list-tile-content>
                  </v-list-tile>
                </template>
              </v-list>
            </v-layout>
          </div>
          <v-alert v-model='fail' color="error" icon="warning">
            Failed to contact this account. Server seems down.
            <br>
            <br> {{error}}
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn @click.native="pingDialog=false">Close</v-btn>
          <v-btn v-if='fail' color='error' @click.native="deleteDialog=true">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>
<script>
import API from '../store/apicaller.js'

export default {
  name: 'Account',
  props: {
    account: Object,
    expanded: Boolean
  },
  computed: {},
  watch: {
    pingDialog( value ) {
      console.log( value + "yo" )
      if ( !value ) return
      // if(this.streams.length > 0) {}
      API.getStreams( this.account )
        .then( res => {
          console.log( res )
          this.fail = false
          this.streams = res.streams
          this.selectedStream = null
        } )
        .catch( err => {
          this.streams = [ ]
          this.fail = true
          this.error = err.toString( )
        } )
    }
  },
  data( ) {
    return {
      deleteDialog: false,
      pingDialog: false,
      pinged: false,
      gotStreams: false,
      streams: [ ],
      fail: false,
      error: null
    }
  },
  methods: {
    deleteAccount( ) {
      Interop.removeAccount( this.account.fileName )
      this.$store.commit( 'DELETE_ACCOUNT', this.account.fileName )
    }
  },
  mounted( ) {
    console.log( "Yo mounted an account thing", this.account.serverName )
  }
}
</script>
<style lang="scss">
</style>