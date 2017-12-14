<template>
  <v-card class='receiver-content'>
    <!-- header - menu and title -->
    <v-layout>
      <!-- speed dial menu -->
      <v-speed-dial v-model='fab' direction='right' left absolute style='top:15px' class='pa-0 ma-0'>
        <v-btn fab small :flat='paused' class='ma-0 light-blue' slot='activator' v-model='fab'>
          <v-icon>
            <!-- cloud_upload -->
            arrow_upward
          </v-icon>
          <v-icon>close</v-icon>
        </v-btn>
        <v-tooltip bottom>
          Add or remove objects from the stream.
          <v-btn fab small class='yellow darken-3 mr-1' slot='activator' @click.native='showAddRemoveDialog = true'>
            <v-icon>swap_horiz</v-icon>
          </v-btn>
        </v-tooltip>
        <v-btn fab small @click.native='togglePause' class=' ma-1'>
          <v-icon>{{ paused ? "pause" : "play_arrow" }}</v-icon>
        </v-btn>
        <v-btn fab small class='red ma-1' @click.native='confirmDelete=true'>
          <v-icon>delete</v-icon>
        </v-btn>
      </v-speed-dial>
      <!-- title -->
      <v-flex>
        <v-card-title primary-title class='pb-0 pt-3 ml-5' :class='{ faded: fab }' style='transition: all .3s ease;'>
          <p class='headline mb-1'>
            {{ client.stream.name }}
          </p>
          <div class='caption'> <span class='grey--text text--darkenx'><code class='grey darken-2 white--text'>{{ client.stream.streamId }}</code> {{paused ? "(paused)" : ""}} updated:
              <timeago :auto-update='10' :since='client.lastUpdate'></timeago></span>
          </div>
        </v-card-title>
      </v-flex>
    </v-layout>
    <!-- progress bar -->
    <v-progress-linear height='1' :indeterminate='true' v-if='client.isLoading'></v-progress-linear>
    <!-- expired alert -->
    <v-alert color='info' v-model='client.expired' class='pb-0 pt-0 mt-3'>
      <v-layout>
        <v-flex class='text-xs-center'>Stream is outdated.
          <v-tooltip left>
            Force refresh.
            <v-btn dark small fab flat @click.native='refreshStream' slot='activator' class='ma-0 '>
              <v-icon>refresh</v-icon>
            </v-btn>
          </v-tooltip>
        </v-flex>
      </v-layout>
    </v-alert>
    <!-- error alert -->
    <v-alert color='error' v-model='hasError' class='mt-4'>
      <v-layout align-center>
        <v-flex>Error: {{ client.error }}
          <v-tooltip left>
            Force refresh.
            <v-btn dark small fab flat @click.native='refreshStream' slot='activator' class='ma-0'>
              <v-icon>refresh</v-icon>
            </v-btn>
          </v-tooltip>
        </v-flex>
      </v-layout>
    </v-alert>
    <!-- standard actions -->
    <v-card-actions v-show='true' class='pl-2'>
      <v-spacer></v-spacer>
      <v-btn icon @click.native='toggleLayers' small>
        <v-icon class='xs-actions'>{{ showLayers ? 'keyboard_arrow_up' : 'layers' }}</v-icon>
      </v-btn>
      <!-- <v-btn icon @click.native='toggleLog' small>
          <v-icon class='xs-actions'>{{ showLog ? 'keyboard_arrow_up' : 'list' }}</v-icon>
        </v-btn> -->
      <v-btn icon @click.native='toggleChildren' small>
        <v-icon class='xs-actions'>{{ showChildren ? 'keyboard_arrow_up' : 'history' }}</v-icon>
      </v-btn>
    </v-card-actions>
    <!-- layers -->
    <v-slide-y-transition>
      <div v-show='showLayers' class='pa-0'>
        <sender-layers :layers='client.stream.layers' :objects='client.stream.objects' :clientId='client.ClientId'></sender-layers>
      </div>
    </v-slide-y-transition>
    <!-- log -->
    <v-slide-y-transition>
      <v-card-text v-show='showLog' class='pa-0'>
        <!-- <blockquote class='section-title'>Log</blockquote> -->
        <div class='caption pa-2'>Client id: <code>{{client.ClientId}}</code></div>
        <div class='log pa-2'>
          <template v-for='log in client.log'>
            <div class='caption' mb-5>
              <v-divider></v-divider>
              {{ log.timestamp }}: {{ log.message }}
            </div>
          </template>
        </div>
        <br>
      </v-card-text>
    </v-slide-y-transition>
    <!-- history -->
    <v-slide-y-transition>
      <v-card-text v-show='showChildren' xxxclass='grey darken-4'>
        History: todo
      </v-card-text>
    </v-slide-y-transition>
    <!-- add objects dialog -->
    <v-dialog fullscreen v-model='showAddRemoveDialog'>
      <v-card>
        <v-toolbar style="flex: 0 0 auto;" dark>
          <v-btn icon @click.native="showAddRemoveDialog = false" dark>
            <v-icon>close</v-icon>
          </v-btn>
          <v-toolbar-title>Add or Remove to {{client.stream.name}}</v-toolbar-title>
        </v-toolbar>
        <v-card-text>
          <div class='headline'>Based on your selection, there are <strong>{{selectionObjectCount}} </strong> objects on {{objectSelection.length}} layers.</div>
          <div class='body-1'>You can still edit your selection.</div>
          <div syle='width:100%' class='pa-3'>
            <template v-for='sel in objectSelection'>
              <v-chip xxxsmall class='eliptic caption' style='text-align: left; max-width: 40%;' slot='activator'>
                <v-avatar :style='{ backgroundColor: sel.color }'>{{sel.objectCount}}</v-avatar>
                {{sel.layerName}}
              </v-chip>
              </v-tooltip>
            </template>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-btn flat @click.native='showAddRemoveDialog=false'>cancel</v-btn>
          <v-spacer></v-spacer>
          <v-btn block color='light-blue' @click.native='addObjectsToStream'>Add</v-btn>
          <v-btn color='red' @click.native='removeObjectsFromStream'>Remove</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
    <!-- confirm delete dialog -->
    <v-dialog v-model='confirmDelete'>
      <v-card>
        <v-card-title class='headline'>Are you sure you want to delete this sender?</v-card-title>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn flat @click.native='confirmDelete=false'>Cancel</v-btn>
          <v-btn color='red' class='' @click.native='removeClient'>Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>
<script>
import SenderLayers from './SenderLayers.vue'

export default {
  name: 'Sender',
  props: {
    client: Object
  },
  components: {
    SenderLayers
  },
  watch: {
    'client.error' ( value ) {
      console.log( "ERRRR", value )
    }
  },
  computed: {
    objectSelection( ) { return this.$store.getters.selection },
    selectionObjectCount( ) {
      let sum = 0
      this.objectSelection.forEach( l => sum += l.objectCount )
      return sum
    },
    layerInfo( ) { return this.$store.getters.layerInfo },
    hasError( ) { return this.client.error != "" && this.client.error != null }
  },
  data( ) {
    return {
      fab: false,
      confirmDelete: false,
      showLayers: false,
      showLog: false,
      showChildren: false,
      showMenu: false,
      showAddRemoveDialog: false,
      paused: false,
    }
  },
  methods: {
    addObjectsToStream( ) {
      let guids = this.objectSelection.reduce( ( acc, obj ) => [ ...obj.ObjectGuids, ...acc ], [ ] )
      Interop.addObjectsToStream( this.client.ClientId, JSON.stringify( guids ) )
    },
    removeObjectsFromStream( ) {

    },
    togglePause( ) {
      this.paused = !this.paused
      Interop.setClientPause( this.client.ClientId, this.paused )
    },
    toggleLog( ) {
      if ( this.showLog ) return this.showLog = false
      this.showLog = true
      this.showLayers = false
      this.showChildren = false
    },
    toggleLayers( ) {
      if ( this.showLayers ) return this.showLayers = false
      this.showLayers = true
      this.showLog = false
      this.showChildren = false
    },
    toggleChildren( ) {
      if ( this.showChildren ) return this.showChildren = false
      this.showLayers = false
      this.showLog = false
      this.showChildren = true
    },
    removeClient( ) {
      this.$store.dispatch( 'removeClient', { clientId: this.client.ClientId } )
    },
    refreshStream( ) {
      this.client.expired = false
      this.killError( )
      Interop.forceSend( this.client.ClientId )
    },
    killError( ) {
      this.client.error = null
    }
  },
  mounted( ) {}
}
</script>
<style lang='scss'>
.faded {
  opacity: 0.2
}

.stream-menu {
  position: absolute;
}

.fat-one {
  /*width:100%;*/
}

.make-me-small {
  font-size: 15px !important;
}
</style>