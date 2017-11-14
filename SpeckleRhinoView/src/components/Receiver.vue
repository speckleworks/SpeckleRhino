<template>
  <v-card class='receiver-content'>
    <v-divider></v-divider>
    <v-card-title primary-title>
      <div>
        <span class='headline'>
          {{ client.stream.name }}
        </span>
        <br>
        <span class='grey--text'>{{ client.stream.streamId }} (receiver) </span>
        <div class='grey--text text--lighten-2 caption'>Last updated at:  <timeago :auto-update='10' :since='client.lastUpdate'></timeago></div>
      </div>
    </v-card-title>
    <v-progress-linear height='3' :indeterminate='true' v-if='client.isLoading'></v-progress-linear>
    <v-alert color='info' v-model='client.expired' >
      <v-layout align-center>
        <v-flex>There are updates available.</v-flex>
        <v-flex><v-btn dark small fab @click.native='refreshStream'><v-icon>refresh</v-icon></v-btn></v-flex>
      </v-layout>
    </v-alert>
    <v-card-actions>
      <v-btn icon @click.native='toggleLayers' fab small dark>
        <v-icon>{{ showLayers ? 'keyboard_arrow_up' : 'layers' }}</v-icon>
      </v-btn>
      <v-btn icon @click.native='toggleLog' fab small dark>
        <v-icon>{{ showLog ? 'keyboard_arrow_up' : 'list' }}</v-icon>
      </v-btn>
      <v-spacer></v-spacer>
      <v-btn icon flat color='yellow lighten-3' small @click.native='bakeClient'>
        <v-icon>play_for_work</v-icon>
      </v-btn>
      <v-btn icon flat small @click.native='toggleVisibility'>
        <v-icon>{{ visible ? "visibility" : "visibility_off" }}</v-icon>
      </v-btn>
      <v-btn icon flat color='light-blue lighten-3' small @click.native='togglePause'>
        <v-icon>{{ paused ?  "pause_circle_outline" : "play_circle_outline" }}</v-icon>
      </v-btn>
      <v-btn icon flat color='red lighten-3' small @click.native='removeClient'>
        <v-icon>close</v-icon>
      </v-btn>
    </v-card-actions>
    <v-slide-y-transition>
      <v-card-text v-show='showLayers' xxxclass='grey darken-4'>
        <blockquote>Layers:</blockquote>
        <receiver-layers :layers='client.stream.layers'></receiver-layers>
      </v-card-text>
    </v-slide-y-transition>
    <v-slide-y-transition>
      <v-card-text v-show='showLog' xxxclass='grey darken-4'>
        <blockquote class=''>Log</blockquote>
        <br>
        <div class='log'>
          <template v-for='log in client.log'> 
            <div class='caption' mb-5>
            <v-divider></v-divider>
            <timeago :since='log.timestamp'></timeago>: {{ log.message }}
            </div>
          </template>
        </div>
        <br>
        <div class='caption'>Client id: <code>{{client.ClientId}}</code></div>
      </v-card-text>
    </v-slide-y-transition>
  </v-card>
</template>

<script>
  import ReceiverLayers from './ReceiverLayers.vue'

  export default {
    name: 'Receiver',
    components: {
      ReceiverLayers
    },
    props: {
      client: Object
    },
    computed: {},
    data() {
      return {
        enableRefresh: false,
        showLayers: false,
        showLog: false,
        visible: true,
        paused: false
      }
    },
    methods: {
      togglePause() {
        this.paused = ! this.paused
        Interop.setClientPause( this.client.ClientId, this.paused )
      },
      toggleVisibility() {
        this.visible = !this.visible
        Interop.setClientVisibility( this.client.ClientId, this.visible )
      },
      bakeClient() {
        Interop.bakeClient( this.ClientId )
      },
      toggleLog() {
        if( this.showLog ) return this.showLog = false
        this.showLog = true
        this.showLayers = false
      },
      toggleLayers() {
        if( this.showLayers ) return this.showLayers = false
        this.showLayers = true
        this.showLog = false
      },
      removeClient() {
        this.$store.dispatch( 'removeClient', { clientId: this.client.ClientId } )
      },
      refreshStream() {
        Interop.refreshClient( this.client.ClientId )
      }
    },
    mounted() {

    }
  }
</script>

<style lang='scss'>

.receiver-content {
  transition: all .3s ease;
}
.receiver-content:hover{ 
  background-color: #1A1A1A;
}

.log {
  max-height: 210px;
  overflow: auto;
}
</style>